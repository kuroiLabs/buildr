using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace buildr.FileSystem
{
	public class Service
	{

		private string root;

		public string Root
		{
			get { return root; }
			private set { root = value; }
		}

		public Service()
		{
			Root = Directory.GetCurrentDirectory();
		}

		public async Task WriteFile(string _path, string _content)
		{
			await File.WriteAllTextAsync(_path, _content);
		}

		public string ReadFile(string _path)
		{
			return File.ReadAllText(_path);
		}

		public T ReadFile<T>(string _path)
		{
			string _content = ReadFile(_path);
			return JsonSerializer.Deserialize<T>(_content);
		}

		public async Task CopyFile(string _source, string _destination)
		{
			try
			{
				string _content = ReadFile(_source);
				if (String.IsNullOrEmpty(_content))
					return;
				await WriteFile(_destination, _content);
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to copy file. {e.Message}");
			}
		}
	}
}
