﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.Common
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class JdbClient : AsyncRedirectProcess.EventListener, IDisposable
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public delegate void OnResultRecordDelegate (MiResultRecord resultRecord);

    public delegate void OnAsyncRecordDelegate (MiAsyncRecord asyncRecord);

    public delegate void OnStreamRecordDelegate (MiStreamRecord streamRecord);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public OnResultRecordDelegate OnResultRecord { get; set; }

    public OnAsyncRecordDelegate OnAsyncRecord { get; set; }

    public OnStreamRecordDelegate OnStreamRecord { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private readonly JdbSetup m_jdbSetup;

    private AsyncRedirectProcess m_jdbClientInstance;

    private Dictionary<uint, OnResultRecordDelegate> m_asyncCommandCallbacks;

    private ManualResetEvent m_syncCommandLock;

    private int m_lastOperationTimestamp;

    private uint m_sessionCommandToken;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public JdbClient (JdbSetup jdbSetup)
    {
      LoggingUtils.PrintFunction ();

      m_jdbSetup = jdbSetup;

      m_jdbClientInstance = null;

      m_asyncCommandCallbacks = new Dictionary<uint, OnResultRecordDelegate> ();

      m_syncCommandLock = null;

      m_sessionCommandToken = 1; // Start at 1 so 0 can represent an invalid token.
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Dispose ()
    {
      LoggingUtils.PrintFunction ();

      SendAsyncCommand ("quit");

      if (m_jdbClientInstance != null)
      {
        m_jdbClientInstance.Dispose ();

        m_jdbClientInstance = null;
      }

      if (m_syncCommandLock != null)
      {
        m_syncCommandLock.Dispose ();

        m_syncCommandLock = null;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool Start ()
    {
      LoggingUtils.PrintFunction ();

      m_jdbSetup.SetupPortForwarding ();

      string [] execCommands = m_jdbSetup.CreateJdbExecutionScript ();

      using (StreamWriter writer = new StreamWriter (Path.Combine (m_jdbSetup.CacheDirectory, "jdb.ini"), false, Encoding.ASCII))
      {
        foreach (string command in execCommands)
        {
          writer.WriteLine (command);
        }
      }

      StringBuilder argumentBuilder = new StringBuilder ();

      //argumentBuilder.Append (" -Duser.home=" + StringUtils.ConvertPathWindowsToPosix (m_jdbSetup.CacheDirectory));

      argumentBuilder.Append (string.Format (" -connect com.sun.jdi.SocketAttach:hostname={0},port={1}", m_jdbSetup.Host, m_jdbSetup.Port));

      m_jdbClientInstance = new AsyncRedirectProcess (Path.Combine (JavaSettings.JdkRoot, @"bin\jdb.exe"), argumentBuilder.ToString ());

      m_jdbClientInstance.Listener = this;

      m_lastOperationTimestamp = Environment.TickCount;

      m_jdbClientInstance.Start ();

      return true;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool Attach ()
    {
      LoggingUtils.PrintFunction ();

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool Detach ()
    {
      LoggingUtils.PrintFunction ();

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Stop ()
    {
      LoggingUtils.PrintFunction ();

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Continue ()
    {
      LoggingUtils.PrintFunction ();

      SendAsyncCommand ("cont");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Terminate ()
    {
      LoggingUtils.PrintFunction ();

      throw new NotImplementedException ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public MiResultRecord SendCommand (string command, int timeout = 30000)
    {
      // 
      // Perform a synchronous command request; issue a standard async command and keep alive whilst still receiving output.
      // 

      LoggingUtils.Print (string.Format ("[JdbClient] SendCommand: {0}", command));

      MiResultRecord syncResultRecord = null;

      if (m_jdbClientInstance == null)
      {
        return syncResultRecord;
      }

      //lock (this)
      {
        m_syncCommandLock = new ManualResetEvent (false);

        SendAsyncCommand (command, delegate (MiResultRecord record)
        {
          syncResultRecord = record;

          if (m_syncCommandLock != null)
          {
            m_syncCommandLock.Set ();
          }
        });

        // 
        // Wait for asynchronous record response (or exit), reset timeout each time new activity occurs.
        // 

        int timeoutFromCurrentTick = (timeout + m_lastOperationTimestamp) - Environment.TickCount;

        bool responseSignaled = false;

        while ((!responseSignaled) && (timeoutFromCurrentTick > 0))
        {
          responseSignaled = m_syncCommandLock.WaitOne (timeoutFromCurrentTick);

          if (!responseSignaled)
          {
            timeoutFromCurrentTick = (timeout + m_lastOperationTimestamp) - Environment.TickCount;

            Thread.Yield ();
          }
        }

        if (!responseSignaled)
        {
          throw new TimeoutException ("Timed out waiting for synchronous SendCommand response.");
        }

        m_syncCommandLock = null;
      }

      return syncResultRecord;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SendAsyncCommand (string command, OnResultRecordDelegate asyncDelegate = null)
    {
      // 
      // Keep track of this command, and associated token-id, so results can be tracked asynchronously.
      // 

      LoggingUtils.Print (string.Format ("[JdbClient] SendAsyncCommand: {0}", command));

      if (m_jdbClientInstance == null)
      {
        return;
      }

      //lock (this)
      {
        m_asyncCommandCallbacks.Add (m_sessionCommandToken, asyncDelegate);

        //command = m_sessionCommandToken + command;

        ++m_sessionCommandToken;

        m_jdbClientInstance.SendCommand (command);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStdout (object sendingProcess, DataReceivedEventArgs args)
    {
      try
      {
        m_lastOperationTimestamp = Environment.TickCount;

        if (!string.IsNullOrEmpty (args.Data))
        {
          LoggingUtils.Print (string.Format ("[JdbClient] ProcessStdout: {0}", args.Data));

          // 
          // Simplistic exception handling.
          // 

          if (args.Data.Contains ("Exception occurred:"))
          {
            Continue ();
          }
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessStderr (object sendingProcess, DataReceivedEventArgs args)
    {
      try
      {
        m_lastOperationTimestamp = Environment.TickCount;

        if (!string.IsNullOrEmpty (args.Data))
        {
          LoggingUtils.Print (string.Format ("[JdbClient] ProcessStderr: {0}", args.Data));
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ProcessExited (object sendingProcess, EventArgs args)
    {
      try
      {
        m_lastOperationTimestamp = Environment.TickCount;

        LoggingUtils.Print (string.Format ("[JdbClient] ProcessExited: {0}", args));

        // 
        // If we're waiting on a synchronous command, signal a finish to process termination.
        // 

        if (m_syncCommandLock != null)
        {
          m_syncCommandLock.Set ();
        }
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
