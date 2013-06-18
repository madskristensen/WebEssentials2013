using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.EditorExtensions
{
    public static class IVsExtensions
    {
        public static void AddHierarchyItem(this ErrorTask task)
        {
            IVsHierarchy HierarchyItem;
            IVsSolution solution = EditorExtensionsPackage.GetGlobalService<IVsSolution>(typeof(SVsSolution));
            Project project = ProjectHelpers.GetActiveProject();

            if (solution != null && project != null)
            {
                int flag = solution.GetProjectOfUniqueName(project.FullName, out HierarchyItem);

                if (0 == flag)
                {
                    task.HierarchyItem = HierarchyItem;
                }
            }
        }

        public static bool IsLink(this ProjectItem item)
        {
            try
            {
                return (bool)item.Properties.Item("IsLink").Value;
            }
            catch
            {
                return false;
            }
        }
    }
}
