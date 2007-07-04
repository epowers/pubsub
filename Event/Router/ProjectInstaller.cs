using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;

namespace Microsoft.WebSolutionsPlatform.Event
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
        /// <summary>
        /// Constructor
        /// </summary>
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}