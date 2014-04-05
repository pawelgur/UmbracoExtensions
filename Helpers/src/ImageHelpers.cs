using Umbraco;
using Umbraco.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web;
using umbraco;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Core.Logging;
using umbraco.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

namespace PG.UmbracoExtensions.Helpers
{
    public static class ImageHelpers
    {
        static HttpServerUtility Server = HttpContext.Current.Server;
        static IMediaService MediaService = ApplicationContext.Current.Services.MediaService;

        /// <summary>
        /// Gets thumbnail url. 
        /// Checks if thumbnail is defined in post, if not - returns crop of main image, if it is absent - default thumbnail image crop.
        /// 
        ///     Priority: 1.Thumbnail 2.Image 3.Default site thumbnail
        /// 
        /// TODO: checking for crops and creating files from coordinates, get solution to helper problem (use services)
        /// </summary>
        /// <param name="post"></param>
        /// <param name="cropName"></param>
        /// <param name="cropThumb">use thumbnail crop if set to true</param>
        /// <param name="useDefaultThumb">use default image if thumb is not found</param>
        /// <returns>thumbnail url</returns>
        public static String GetThumbnailUrl(IPublishedContent post, String cropName = "", bool cropThumb = false)
        {
            String result = "";
            
            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            try
            {
                var thumbnailID = post.GetPropertyValue("thumbnail");
                if (thumbnailID == null || thumbnailID == String.Empty)
                {
                    //no thumbnail
                    var pictureID = post.GetPropertyValue("image");
                    if (pictureID != String.Empty && pictureID != null)
                    {
                        var picture = umbracoHelper.Media(pictureID);
                        if (cropName != "")
                        {
                            var thumbnail = picture.AsDynamic().imageCropper.crops.Find("@name", cropName);
                            result = thumbnail.url;
                        }

                    }
                    else
                    {
                        //return default thumb
                        IMediaService mediaService = ApplicationContext.Current.Services.MediaService;
                        IEnumerable<IMedia> buffer = mediaService.GetByLevel(1).Where(x => x.Name == "Default");
                        if (buffer.Any())
                        {
                            IMedia defaultFolder = buffer.First();
                            buffer = mediaService.GetChildren(defaultFolder.Id);
                            if (buffer.Any())
                            {
                                buffer = buffer.Where(x => x.Name == "default-thumbnail");
                                if (buffer.Any() && cropName != "")
                                {
                                    IMedia defaultThumb = buffer.First();
                                    result = GetCrop(defaultThumb.GetValue<String>("imageCropper"), cropName);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var thumbnail = umbracoHelper.Media(thumbnailID);
                    result = thumbnail.Url;
                    if (cropThumb && cropName != "")
                    {
                        thumbnail = thumbnail.AsDynamic().imageCropper.crops.Find("@name", cropName);
                        result = thumbnail.url;
                    }

                }
            }
            catch (Exception e)
            {
                if (UmbracoContext.Current.IsDebug)
                {
                    //throw e;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets main image url.
        /// 
        /// Gets crop if cropname specified
        /// </summary>
        /// <param name="post"></param>
        /// <param name="umbracoHelper"></param>
        /// <param name="cropName"></param>
        /// <returns></returns>
        public static String GetImageUrl(IPublishedContent post, String cropName = "", String fieldAlias = "image")
        {
            String result = "";

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            try
            {
                var pictureID = post.GetPropertyValue(fieldAlias);
                

                if (pictureID != String.Empty && pictureID != null)
                {
                    int id = Int32.Parse(pictureID.ToString());
                    result = GetImageUrl(id, cropName);
                }

            }
            catch (Exception e)
            {
                if (UmbracoContext.Current.IsDebug)
                {
                    //throw e;
                }
            }

            return result;
        }


        /// <summary>
        /// Gets the specified crop from ImageCropper string value
        /// </summary>
        /// <param name="cropperValue"></param>
        /// <param name="croppName"></param>
        /// <returns></returns>
        public static String GetCrop(String cropperValue, String croppName)
        {
            String result = "";

            System.Xml.Linq.XDocument xmlDoc = System.Xml.Linq.XDocument.Parse(cropperValue);

            var queryResult = from crop in xmlDoc.Descendants("crop")
                              where (string)crop.Attribute("name") == croppName
                              select new
                              {
                                  url = (string)crop.Attribute("url")
                              };
            foreach (var t in queryResult)
            {
                result = t.url;
                break;
            }


            return result;
        }


        /// <summary>
        /// Creates QR code using Google chart api.
        /// Returns url of created qr code.
        /// </summary>
        /// <param name="dataToEncode"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static String GetQrCodeUrl(String dataToEncode, int width, int height)
        {
            String result = "";

            var url = string.Format("http://chart.apis.google.com/chart?cht=qr&chl={0}&chs={1}x{2}", HttpUtility.UrlEncode(dataToEncode), width, height);
            var mediaService = ApplicationContext.Current.Services.MediaService;

            result = url;

            //get qr code folder
            IMedia qrCodeFolder = null;
            var otherFolder = mediaService.GetRootMedia().FirstOrDefault(x => x.Name == "Other");
            if (otherFolder != null)
            {
                qrCodeFolder = otherFolder.Children().FirstOrDefault(x => x.Name == "QR codes");
                if (qrCodeFolder == null)
                {
                    qrCodeFolder = mediaService.CreateMedia("QR codes", otherFolder, "Folder");
                    mediaService.Save(qrCodeFolder);
                }
            }
            if (qrCodeFolder != null)
            {
                String mediaName = dataToEncode.Substring(dataToEncode.Length > 40 ? dataToEncode.Length - 40 : 0);

                IMedia existingCode = qrCodeFolder.Children().FirstOrDefault(x => x.Name == mediaName);

                if (existingCode == null)
                {
                    try
                    {
                        //create media item
                        IMedia qrCode = mediaService.CreateMedia(mediaName, qrCodeFolder, "Image");

                        //update id
                        mediaService.Save(qrCode);
                        qrCode = qrCodeFolder.Children().FirstOrDefault(x => x.Name == mediaName);

                        //create folder
                        String mediaRootPath = "~/media/";
                        String folderPath = mediaRootPath + qrCode.Id;
                        System.IO.Directory.CreateDirectory(Server.MapPath(folderPath));

                        //download file
                        String fileName = mediaName.ReplaceMany(new char[] { '/', '\\', '\'', '\"', ',', '#' }, '-') + qrCode.Id.ToString() + ".png";
                        String filePath = Server.MapPath(folderPath + "/" + fileName);

                        WebClient webClient = new WebClient();
                        webClient.DownloadFile(url, filePath);

                        //set media item properties
                        System.IO.FileInfo info = new FileInfo(filePath);
                        qrCode.Properties["umbracoExtension"].Value = "png";
                        qrCode.Properties["umbracoBytes"].Value = info.Length.ToString();
                        qrCode.Properties["umbracoFile"].Value = "/media/" + qrCode.Id + "/" + fileName;
                        qrCode.Properties["umbracoWidth"].Value = width.ToString();
                        qrCode.Properties["umbracoHeight"].Value = height.ToString();

                        mediaService.Save(qrCode);

                        result = (String)qrCode.Properties["umbracoFile"].Value;
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error(typeof(IMedia), "Error creating qr code for data: " + dataToEncode, e);
                    }
                }
                else
                {
                    result = (String)existingCode.Properties["umbracoFile"].Value;
                }

            }

            return result;
        }

        /// <summary>
        /// Gets images url list.
        /// 
        /// Gets crops if cropname specified
        /// </summary>
        /// <param name="post"></param>
        /// <param name="cropName"></param>
        /// <returns></returns>
        public static IEnumerable<String> GetImagesUrl(IPublishedContent post, String cropName = "", String fieldAlias = "images")
        {
            List<string> result = new List<string>();

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            try
            {
                var pictureIDsVal = post.GetPropertyValue<String>(fieldAlias);
                if (!String.IsNullOrEmpty(pictureIDsVal))
                {
                    var pictureIDs = pictureIDsVal.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pictureIDstr in pictureIDs)
                    {
                        if (!String.IsNullOrEmpty(pictureIDstr))
                        {
                            int id = Int32.Parse(pictureIDstr);
                            var url = GetImageUrl(id, cropName);
                            result.Add(url);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                if (UmbracoContext.Current.IsDebug)
                {
                    //throw e;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets image url by its id
        /// </summary>
        /// <param name="pictureID"></param>
        /// <param name="cropName"></param>
        /// <returns></returns>
        public static string GetImageUrl(int pictureID, string cropName = "")
        {
            var result = "";

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            var picture = umbracoHelper.TypedMedia(pictureID);
            if (picture != null)
            {
                result = picture.GetPropertyValue<String>("umbracoFile");
                if (cropName != "")
                {
                    var crop = picture.AsDynamic().imageCropper.crops.Find("@name", cropName);
                    result = crop.url;
                }
            }

            return result;
        }
        
    }
}
