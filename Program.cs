namespace CopilotInteractionApp
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			Application.ThreadException += (_, e) => ShowFatal(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowFatal(e.ExceptionObject as Exception);

			ApplicationConfiguration.Initialize();
			Application.Run(new Form1());
		}

		private static void ShowFatal(Exception? exception)
		{
			MessageBox.Show(
				exception?.ToString() ?? "An unknown error occurred.",
				"Unexpected error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}
}
