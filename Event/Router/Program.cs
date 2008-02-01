using System.Collections.Generic;
using System.ServiceProcess;
using System.Diagnostics;
using System.Text;

namespace Microsoft.WebSolutionsPlatform.Event
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
            // ksh Un-comment the following lines to start manually

            // Router eventsvc = new Router();
            // eventsvc.Start();
            // return;

			ServiceBase[] ServicesToRun;

			// More than one user Service may run within the same process. To add
			// another service to this process, change the following line to
			// create a second service object. For example,
			//
			//   ServicesToRun = new ServiceBase[] {new Service1(), new MySecondUserService()};
			//
			ServicesToRun = new ServiceBase[] { new Router() };

			ServiceBase.Run(ServicesToRun);
		}
	}
}