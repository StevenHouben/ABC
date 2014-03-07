﻿using System;
using System.Collections.Generic;
using System.Linq;
using ABC.Common;
using ABC.PInvoke.Process;
using Whathecode.System;


namespace ABC.Applications.Persistence
{
	/// <summary>
	///   Provides persistence providers for supported applications.
	/// </summary>
	public abstract class AbstractPersistenceProvider : AbstractDisposable
	{
		readonly ProcessTracker _processTracker;
		// TODO: Persist the currently tracked command line parameters to allow suspending upon application restart.
		readonly Dictionary<int, string> _commandLine = new Dictionary<int, string>();


		protected AbstractPersistenceProvider()
		{
			// Set up the process tracker which checks with which command line arguments processes were launched.
			_processTracker = new ProcessTracker();
			_processTracker.ProcessStarted += p =>
			{
				_commandLine[ p.Id ] = p.CommandLine;
			};
			_processTracker.ProcessStopped += p => _commandLine.Remove( p.Id );
			_processTracker.Start();
		}


		public List<PersistedApplication> Suspend( List<IWindow> windows )
		{
			var persistedApplications = (
				from processWindows in windows.GroupBy( w => w.GetProcess() )
				let process = processWindows.Key
				where process != null
				let applicationPath = process.Modules[ 0 ].FileName
				let persistor = GetPersistenceProviders().FirstOrDefault( p => p.ProcessName == process.ProcessName )
				where persistor != null
				let persistedData = persistor.Suspend(
					new SuspendInformation
					{
						Process = process,
						Windows = processWindows.ToList(),
						CommandLine = _commandLine.FirstOrDefault( a => a.Key == process.Id ).Value
					} )
				select
					new PersistedApplication( process, processWindows.ToList(), persistedData )
					{
						ApplicationPath = applicationPath,
						Persistor = persistor.GetType().AssemblyQualifiedName
					} ).ToList();

			persistedApplications.ForEach( p => _commandLine.Remove( p.Process.Id ) );

			return persistedApplications;
		}

		public void Resume( List<PersistedApplication> persistedStates )
		{
			foreach ( var s in persistedStates )
			{
				AbstractApplicationPersistence persistor = GetPersistenceProviders().FirstOrDefault( p => p.GetType().AssemblyQualifiedName == s.Persistor );
				if ( persistor != null )
				{
					persistor.Resume( s.ApplicationPath, s.Data );
				}
			}
		}
		protected override void FreeManagedResources()
		{
			_processTracker.Dispose();
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		/// <summary>
		///   Returns the types which are used to store persisted data. This needs to be passed to the DataContractSerializer.
		/// </summary>
		/// <returns></returns>
		public List<Type> GetPersistedDataTypes()
		{
			return GetPersistenceProviders().Select( p => p.GetPersistedDataType() ).ToList();
		}


		protected abstract List<AbstractApplicationPersistence> GetPersistenceProviders();
	}
}
