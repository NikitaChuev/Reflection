using System.IO;

namespace FileReflection
{
    public class FileViewModel : FileSystemViewModel, IReflectable
    {
        public FileViewModel(string name, long size)
        {
            Name = name;
            Size = size;
            ItemType = "File";
        }

        public FileViewModel(FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            Size = fileInfo.Length;
            ItemType = "File";
        }

        public void TestMethod()
        {
            
        }
    }
}
