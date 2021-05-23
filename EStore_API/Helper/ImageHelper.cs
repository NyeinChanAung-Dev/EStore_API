using EStore_API.Models.ViewModel.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EStore_API.Helper
{
    public static class ImageHelper
    {
        public static string GetFullFilePath(string folder, string fileName)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName))
                return "";

            var saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\", folder);
            var fullFilePath = Path.Combine(saveFilePath, fileName);

            if (!Directory.Exists(saveFilePath))
            {
                Directory.CreateDirectory(saveFilePath);
            }

            return fullFilePath;
        }
    }
}
