using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMateSync
{
    class Store
    {
        LiteDatabase _db;
        public Store(LiteDatabase db)
        {
            _db = db;
        }

        LiteCollection<FileItem> GetFileItemCollection()
        {
            return _db.GetCollection<FileItem>("fileitems");
        }

        LiteCollection<ServerItem> GetServerItemCollection()
        {
            return _db.GetCollection<ServerItem>("serveritems");
        }

        public void SaveServerItem(ServerItem sitem)
        {
            var col = GetServerItemCollection();
            if (col.Exists(x => x.Path.Equals(sitem.Path)))
                return;
            col.Insert(sitem);
        }

        public void SaveServer(DirectoryInfo di)
        {
            var sitem = new ServerItem { Path = di.FullName };
            SaveServerItem(sitem);
        }

        public IEnumerable<ServerItem> FindAllServerItem()
        {
            return GetServerItemCollection().FindAll();
        }

        public void SaveFileItem(FileItem fitem)
        {
            var col = GetFileItemCollection();

            var old = col.FindOne(x => x.Path.Equals(fitem.Path));
            if(old != null)
            {
                old.LastWrite = fitem.LastWrite;
                col.Update(old);
                return;
            }
            col.Insert(fitem);
        }

        public FileItem FindFileItem(string path)
        {
            var col = _db.GetCollection<FileItem>("fileitems");
            return col.Find(x => x.Path.Equals(path)).FirstOrDefault();
        }

    }
}
