﻿////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Interop;
using AndroidPlusPlus.Common;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace AndroidPlusPlus.VsDebugEngine
{

  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  public class CLangDebuggeeProgram : IDebugProgram3
  {

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected readonly CLangDebugger m_debugger;

    protected List<DebuggeeModule> m_debugModules;

    protected List<DebuggeeThread> m_debugThreads;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeProgram (CLangDebugger debugger, DebuggeeProgram debugProgram)
    {
      m_debugger = debugger;

      DebugProgram = debugProgram;

      IsRunning = true;

      m_debugModules = new List<DebuggeeModule> ();

      m_debugThreads = new List<DebuggeeThread> ();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public DebuggeeProgram DebugProgram { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsRunning { get; protected set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public uint CurrentThreadId { get; set; }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectThread (CLangDebuggeeThread thread)
    {
      LoggingUtils.PrintFunction ();

      uint requestedThreadId;

      LoggingUtils.RequireOk (thread.GetThreadId (out requestedThreadId));

      if (requestedThreadId != CurrentThreadId)
      {
        m_debugger.GdbClient.SendCommand ("-thread-select " + requestedThreadId);

        CurrentThreadId = requestedThreadId;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void AddModule (CLangDebuggeeModule module)
    {
      LoggingUtils.PrintFunction ();

      if (module == null)
      {
        throw new ArgumentNullException ("module");
      }

      lock (m_debugModules)
      {
        if (m_debugModules.Contains (module))
        {
          throw new ArgumentException ();
        }

        m_debugModules.Add (module);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeModule GetModule (string moduleName)
    {
      LoggingUtils.PrintFunction ();

      lock (m_debugModules)
      {
        foreach (DebuggeeModule module in m_debugModules)
        {
          if (module.Name.Equals (moduleName))
          {
            return module as CLangDebuggeeModule;
          }
        }
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public List<DebuggeeModule> GetModules ()
    {
      LoggingUtils.PrintFunction ();

      return m_debugModules;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void RemoveModule (CLangDebuggeeModule module)
    {
      LoggingUtils.PrintFunction ();

      if (module == null)
      {
        throw new ArgumentNullException ("module");
      }

      lock (m_debugModules)
      {
        if (!m_debugModules.Contains (module))
        {
          throw new ArgumentException ();
        }

        m_debugModules.Remove (module);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void AddThread (CLangDebuggeeThread thread)
    {
      LoggingUtils.PrintFunction ();

      if (thread == null)
      {
        throw new ArgumentNullException ("thread");
      }

      lock (m_debugThreads)
      {
        if (m_debugThreads.Contains (thread))
        {
          throw new ArgumentException ();
        }

        m_debugThreads.Add (thread);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public CLangDebuggeeThread GetThread (uint threadId)
    {
      LoggingUtils.PrintFunction ();

      lock (m_debugThreads)
      {
        foreach (DebuggeeThread thread in m_debugThreads)
        {
          uint currThreadId;

          if ((thread.GetThreadId (out currThreadId) == DebugEngineConstants.S_OK) && (currThreadId == threadId))
          {
            return thread as CLangDebuggeeThread; 
          }
        }
      }

      return null;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public List<DebuggeeThread> GetThreads ()
    {
      LoggingUtils.PrintFunction ();

      return m_debugThreads;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void RemoveThread (CLangDebuggeeThread thread)
    {
      LoggingUtils.PrintFunction ();

      if (thread == null)
      {
        throw new ArgumentNullException ("thread");
      }

      lock (m_debugThreads)
      {
        if (!m_debugThreads.Contains (thread))
        {
          throw new ArgumentException ();
        }

        m_debugThreads.Remove (thread);
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetRunning (bool isRunning)
    {
      LoggingUtils.PrintFunction ();

      IsRunning = isRunning;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugProgram2 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Attach (IDebugEventCallback2 pCallback)
    {
      // 
      // Attaches to this program.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
#if false
        if (!m_debugger.GdbServer.Start ())
        {
          throw new InvalidOperationException ("Could not start gdbserver");
        }

        m_debugger.GdbClient.SendCommand ("-list-features");

        m_debugger.GdbClient.Attach (m_debugger.GdbServer);
#else
        m_debugger.Engine.Broadcast (new CLangDebuggerEvent.StartServer (m_debugger), DebugProgram, null);

        m_debugger.Engine.Broadcast (new CLangDebuggerEvent.AttachClient (m_debugger), DebugProgram, null);
#endif

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int CanDetach ()
    {
      LoggingUtils.PrintFunction ();

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int CauseBreak ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
#if false
        m_debugger.GdbClient.Stop ();
#else
        m_debugger.Engine.Broadcast (new CLangDebuggerEvent.StopClient (m_debugger), DebugProgram, null);
#endif

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Continue (IDebugThread2 pThread)
    {
      LoggingUtils.PrintFunction ();

      try
      {
        if (!IsRunning)
        {
#if false
          m_debugger.GdbClient.Continue ();
#else
          m_debugger.Engine.Broadcast (new CLangDebuggerEvent.ContinueClient (m_debugger), DebugProgram, null);
#endif
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Detach ()
    {
      LoggingUtils.PrintFunction ();

      try
      {
#if false
        m_debugger.GdbClient.Detach ();

        m_debugger.GdbServer.Stop ();
#else
        m_debugger.Engine.Broadcast (new CLangDebuggerEvent.DetachClient (m_debugger), DebugProgram, null);

        m_debugger.Engine.Broadcast (new CLangDebuggerEvent.StopServer (m_debugger), DebugProgram, null);
#endif
        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumCodeContexts (IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
    {
      // 
      // Enumerates the code contexts for a given position in a source file.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return DebugEngineConstants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumCodePaths (string pszHint, IDebugCodeContext2 pStart, IDebugStackFrame2 pFrame, int fSource, out IEnumCodePaths2 ppEnum, out IDebugCodeContext2 ppSafety)
    {
      // 
      // Enumerates the code paths of this program.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        ppSafety = null;

        return DebugEngineConstants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumModules (out IEnumDebugModules2 ppEnum)
    {
      // 
      // Enumerates the modules that this program has loaded and is executing.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        List<IDebugModule2> modules = new List<IDebugModule2> ();

        foreach (DebuggeeModule module in m_debugModules)
        {
          modules.Add (module as IDebugModule2);
        }

        ppEnum = new DebuggeeModule.Enumerator (modules);

        if (ppEnum == null)
        {
          throw new InvalidOperationException ();
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EnumThreads (out IEnumDebugThreads2 ppEnum)
    {
      // 
      // Enumerates the threads that are running in this program.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        List<IDebugThread2> threads = new List<IDebugThread2> ();

        foreach (DebuggeeThread thread in m_debugThreads)
        {
          threads.Add (thread as IDebugThread2);
        }

        ppEnum = new DebuggeeThread.Enumerator (threads);

        if (ppEnum == null)
        {
          throw new InvalidOperationException ();
        }

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        ppEnum = null;

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetDebugProperty (out IDebugProperty2 ppProperty)
    {
      // 
      // Gets program properties.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        ppProperty = null;

        return DebugEngineConstants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Execute ()
    {
      // 
      // Continues running this program from a stopped state. Any previous execution state is cleared.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        LoggingUtils.RequireOk (Continue (GetThread (CurrentThreadId)));

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetDisassemblyStream (enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 pCodeContext, out IDebugDisassemblyStream2 ppDisassemblyStream)
    {
      LoggingUtils.PrintFunction ();

      ppDisassemblyStream = new CLangDebuggeeDisassemblyStream (m_debugger, dwScope, pCodeContext);

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetENCUpdate (out object ppUpdate)
    {
      // 
      // Gets the Edit and Continue (ENC) update for this program.
      // A custom debug engine does not implement this method (it should always return E_NOTIMPL).
      // 

      LoggingUtils.PrintFunction ();

      ppUpdate = null;

      return DebugEngineConstants.E_NOTIMPL;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetEngineInfo (out string pbstrEngine, out Guid pguidEngine)
    {
      // 
      // Gets the name and identifier of the debug engine (DE) running a program.
      // 

      LoggingUtils.PrintFunction ();

      pguidEngine = DebugEngineGuids.guidDebugEngineID;

      pbstrEngine = DebugEngineGuids.GetEngineNameFromId (pguidEngine);

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetMemoryBytes (out IDebugMemoryBytes2 ppMemoryBytes)
    {
      // 
      // Gets the memory bytes for this program.
      // 

      LoggingUtils.PrintFunction ();

      if (m_debugger.NativeMemoryBytes != null)
      {
        ppMemoryBytes = m_debugger.NativeMemoryBytes;

        return DebugEngineConstants.S_OK;
      }

      ppMemoryBytes = null;

      return DebugEngineConstants.S_GETMEMORYBYTES_NO_MEMORY_BYTES;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetName (out string pbstrName)
    {
      // 
      // Gets the name of the program.
      // 

      LoggingUtils.PrintFunction ();

      pbstrName = DebugProgram.DebugProcess.NativeProcess.Name;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetProcess (out IDebugProcess2 ppProcess)
    {
      // 
      // Gets the process that this program is running in.
      // 

      LoggingUtils.PrintFunction ();

      ppProcess = DebugProgram.DebugProcess;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int GetProgramId (out Guid pguidProgramId)
    {
      // 
      // Gets a globally unique identifier for this program.
      // 

      LoggingUtils.PrintFunction ();

      pguidProgramId = DebugProgram.Guid;

      return DebugEngineConstants.S_OK;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Step (IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
    {
      // 
      // Performs a step.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        CLangDebuggeeThread thread = pThread as CLangDebuggeeThread;

        GdbClient.StepType stepType = (GdbClient.StepType)Step;

        SelectThread (thread);

        switch (sk)
        {
          case enum_STEPKIND.STEP_INTO:
          {
            m_debugger.GdbClient.StepInto (stepType, false);

            break;
          }
          case enum_STEPKIND.STEP_OVER:
          {
            m_debugger.GdbClient.StepOver (stepType, false);

            break;
          }
          case enum_STEPKIND.STEP_OUT:
          {
            m_debugger.GdbClient.StepOut (stepType, false);

            break;
          }
          case enum_STEPKIND.STEP_BACKWARDS:
          {
            throw new NotImplementedException ();
          }
        }

        return DebugEngineConstants.S_OK;
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_NOTIMPL;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Terminate ()
    {
      // 
      // Terminates this program.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
#if false
        m_debugger.GdbClient.Terminate ();
#else
        m_debugger.Engine.Broadcast (new CLangDebuggerEvent.TerminateClient (m_debugger), DebugProgram, null);
#endif

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int WriteDump (enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
    {
      // 
      // Writes a dump to a file.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        throw new NotImplementedException ();
      }
      catch (NotImplementedException e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_NOTIMPL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region IDebugProgram3 Members

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int ExecuteOnThread (IDebugThread2 pThread)
    {
      // 
      // Executes the debugger program. The thread is returned to give the debugger information on which thread the user is viewing when executing the program.
      // 

      LoggingUtils.PrintFunction ();

      try
      {
        SelectThread (pThread as CLangDebuggeeThread);

        LoggingUtils.RequireOk (Execute ());

        return DebugEngineConstants.S_OK;
      }
      catch (Exception e)
      {
        LoggingUtils.HandleException (e);

        return DebugEngineConstants.E_FAIL;
      }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #endregion

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
