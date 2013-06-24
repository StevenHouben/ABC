﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace MonitorVdmClient.ViewModel
{
	[ViewModel( typeof( Binding.Properties ), typeof( Binding.Commands ) ) ]
	class MainViewModel
	{
		readonly Model.MonitorVdmClient _vdm = new Model.MonitorVdmClient();

		[NotifyProperty( Binding.Properties.VirtualDesktops )]
		public ObservableCollection<VirtualDesktopViewModel> VirtualDesktops { get; private set; }

		[NotifyProperty( Binding.Properties.WindowClipboard )]
		public ObservableCollection<WindowViewModel> WindowClipboard { get; private set; }

		[NotifyProperty( Binding.Properties.SelectedDesktop )]
		public VirtualDesktopViewModel SelectedDesktop { get; set; }


		public MainViewModel()
		{
			VirtualDesktops = new ObservableCollection<VirtualDesktopViewModel>();
		}


		// ReSharper disable UnusedMember.Local
		[NotifyPropertyChanged( Binding.Properties.SelectedDesktop )]
		void OnSelectedDesktopChanged( VirtualDesktopViewModel oldDesktop, VirtualDesktopViewModel newDesktop )
		{
			if ( oldDesktop == null )
			{
				return;
			}

			oldDesktop.SelectedWindows.Clear();
		}
		// ReSharper restore UnusedMember.Local


		[CommandExecute( Binding.Commands.Show )]
		public void Show()
		{
			SelectedDesktop.ShowSelectedWindows();
		}

		[CommandExecute( Binding.Commands.Hide )]
		public void Hide()
		{
			SelectedDesktop.HideSelectedWindows();
		}

		[CommandExecute( Binding.Commands.Cut )]
		public void Cut()
		{
			_vdm.CutWindows( SelectedDesktop.SelectedWindows.Select( w => w.Window ).ToList() );
		}

		[CommandCanExecute( Binding.Commands.Show )]
		[CommandCanExecute( Binding.Commands.Hide )]
		[CommandCanExecute( Binding.Commands.Cut )]
		public bool AreWindowsSelected()
		{
			return SelectedDesktop != null && SelectedDesktop.SelectedWindows.Count > 0;
		}

		[CommandExecute( Binding.Commands.Refresh )]
		public void Refresh()
		{
			if ( !_vdm.UpdateData() )
			{
				MessageBox.Show( "No Virtual Desktop Manager using the Virtual Desktop Manager API is currently running.", "No VDM found", MessageBoxButton.OK, MessageBoxImage.Exclamation );
				return;
			}

			int desktopNumber = 1;
			var desktops = _vdm.VirtualDesktops.Select(
				d => new VirtualDesktopViewModel( "Desktop" + (desktopNumber++).ToString(), d.Select( w => new WindowViewModel( w ) ).ToList() ) );
			VirtualDesktops = new ObservableCollection<VirtualDesktopViewModel>( desktops );
			WindowClipboard = new ObservableCollection<WindowViewModel>( _vdm.WindowClipboard.Select( w => new WindowViewModel( w ) ).ToList() );
		}
	}
}
