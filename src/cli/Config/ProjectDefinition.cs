namespace buildr.Config
{
	public class ProjectDefinition
	{

		public string type { get; set; }

		public string name { get; set; }

		public string[] dependencies { get; set; }

		public string buildCommand { get; set; }

		public string debugCommand { get; set; }

		public ProjectDefinition()
		{

		}

	}
}
