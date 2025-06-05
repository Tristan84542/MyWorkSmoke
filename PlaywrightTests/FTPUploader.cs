using System;
using System.IO;
using System.Net;

class FtpUploader
{
    public void UploadFile(string ftpUrl, string filePath, string username, string password)
    {
        string fileName = Path.GetFileName(filePath);
        string ftpFullPath = $"{ftpUrl}/{fileName}";

        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFullPath);
        request.Method = WebRequestMethods.Ftp.UploadFile;

        request.Credentials = new NetworkCredential(username, password);
        request.UseBinary = true;
        request.UsePassive = true;
        request.EnableSsl = false;  // Set to true if using FTPS

        // Read the file to upload
        byte[] fileContents = File.ReadAllBytes(filePath);
        request.ContentLength = fileContents.Length;

        // Upload the file
        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(fileContents, 0, fileContents.Length);
        }

        // Get the response
        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        {
            Console.WriteLine($"Upload File Complete, status: {response.StatusDescription}");
        }
    }
}
