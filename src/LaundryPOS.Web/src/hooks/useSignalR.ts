import { useEffect, useRef, useCallback } from 'react';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';

export function useSignalR(hubUrl: string, handlers: Record<string, (...args: unknown[]) => void>) {
  const connectionRef = useRef<HubConnection | null>(null);

  const connect = useCallback(async () => {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    // Register handlers
    Object.entries(handlers).forEach(([event, handler]) => {
      connection.on(event, handler);
    });

    try {
      await connection.start();
      connectionRef.current = connection;
    } catch (err) {
      console.error('SignalR connection failed:', err);
    }
  }, [hubUrl, handlers]);

  useEffect(() => {
    connect();
    return () => {
      connectionRef.current?.stop();
    };
  }, [connect]);

  return connectionRef;
}
