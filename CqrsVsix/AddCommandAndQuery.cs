using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using System.Linq;
using System.Windows.Forms;

namespace CqrsVsix
{
    internal sealed class AddCommandAndQuery
    {
        private DTE _dte { get; set; }
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("6247f181-2f18-49c9-90e2-78fd92bc292c");
        private readonly AsyncPackage package;

        private AddCommandAndQuery(AsyncPackage package, OleMenuCommandService commandService, DTE dte)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _dte = dte;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in AddCommandAndQuery's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = (await package.GetServiceAsync(typeof(DTE))) as DTE;

            new AddCommandAndQuery(package, commandService, dte);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //current file
            ProjectItem currentFile = _dte.SelectedItems.Item(1).ProjectItem;
            // project name
            var projectName = currentFile.ContainingProject.Name;
            var platformCodeProjectName = projectName.Substring(0, projectName.Length - 4);
            Project destinationProject = null;
            if (projectName.EndsWith(".Api"))
            {
                var filePath = (string)currentFile.Properties.Item("LocalPath").Value;
                var relativePath = filePath.Substring(filePath.IndexOf(projectName));
                var directories = relativePath.Split('\\').Skip(1);
                foreach (var project in _dte.Solution.Projects)
                {
                    if (project is Project)
                    {
                        var projectElement = project as Project;
                        if (projectElement.Name == platformCodeProjectName)
                        {
                            destinationProject = projectElement;
                            break;
                        }
                    }
                }

                // add folders and files
                var cursorProjectItem = destinationProject.ProjectItems;
                foreach (var directory in directories)
                {
                    if (!directory.EndsWith(".cs"))
                    {
                        cursorProjectItem = cursorProjectItem.AddFolderIfDoesntExist(directory).ProjectItems;
                    }
                    else
                    {
                        var isCommandHandler = currentFile.Name.Contains("CommandHandler");
                        var objectName = currentFile.Name.Substring(1, currentFile.Name.IndexOf((isCommandHandler ? "CommandHandler" : "QueryHandler"))-1);
                        var commandProject = cursorProjectItem.AddFolderIfDoesntExist((isCommandHandler ? "Commands" : "Queries"));
                        var dataAccess = cursorProjectItem.AddFolderIfDoesntExist("DataAccess");
                        var mappers = cursorProjectItem.AddFolderIfDoesntExist("Mappers");

                        commandProject
                            .GenerateFromTemplateIfDoesntExist($"C:\\Code\\CqrsTemplate\\{(isCommandHandler ? "CommandHandler" : "QueryHandler")}.cs",
                            projectName + "." + String.Join(".", directories.Take(directories.Count() - 1)),
                            objectName);

                        dataAccess
                            .GenerateFromTemplateIfDoesntExist($"C:\\Code\\CqrsTemplate\\{(isCommandHandler ? "Writer" : "Reader")}.cs",
                            projectName + "." + String.Join(".", directories.Take(directories.Count() - 1)),
                            objectName);

                        mappers
                             .GenerateFromTemplateIfDoesntExist("C:\\Code\\CqrsTemplate\\RecordsetMapper.cs",
                             projectName + "." + String.Join(".", directories.Take(directories.Count() - 1)),
                             objectName);
                    }
                }
            }
            else
            {
                MessageBox.Show("This command must be run on an .API project");
            }
        }
    }
}
