using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Text;

namespace CqrsVsix
{
    public static class ProjectsHelper
    {
        public static ProjectItem AddFolderIfDoesntExist(this ProjectItems projectItem, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ProjectItem lastItem = null;
            foreach (ProjectItem item in projectItem)
            {
                if (item.Name == name)
                {
                    return item;
                }
                lastItem = item;
            }

            try
            {
                return projectItem.AddFolder(name);
            } 
            catch
            {
                var anyFilePath = Path.GetDirectoryName((string)lastItem.Properties.Item("LocalPath").Value);
                return projectItem.AddFromDirectory(Path.Combine(anyFilePath, name));
            }
        }

        public static ProjectItem GenerateFromTemplateIfDoesntExist(this ProjectItem project, string templateUrl, string fullNamespace, string baseName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var templateName = Path.GetFileName(templateUrl).Replace(".cs", "");
            var destinationFileName = $"{baseName}{templateName}.cs";
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.FileNames[0] == destinationFileName)
                {
                    return item;
                }
            }

            var anyFilePath = Path.GetDirectoryName((string)project.Properties.Item("LocalPath").Value);
            var destinationFullPath = Path.Combine(anyFilePath, destinationFileName);
            using (FileStream fs = File.Create(destinationFullPath))
            {
                var generatedFile = File.ReadAllText(templateUrl).Replace("{NAMESPACE}", fullNamespace).Replace("{NAME}", baseName);
                var bytes = Encoding.UTF8.GetBytes(generatedFile);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }

            return project.ProjectItems.AddFromFile(destinationFullPath);
        }

    }
}
