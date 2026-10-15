using System;
using System.Collections.Generic;

namespace SecureFileManager.Domain.Entities
{
    public class Folder
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid? ParentFolderId { get; set; }
        public Folder? Parent { get; set; }
        public List<Folder> Children { get; set; } = new();
        public List<FileRecord> Files { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
