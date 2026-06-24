namespace Neptune.EFModels.Entities
{
    public partial class TreatmentBMPImage : IFileResourcePhoto
    {

        public DateTime GetCreateDate()
        {
            return FileResource.CreateDate;
        }

        public string GetCaptionOnFullView()
        {
            return $"{Caption}";
        }

        public string GetCaptionOnGallery()
        {
            return Caption;
        }

        public string GetPhotoUrl()
        {
            return FileResource.GetFileResourceUrl();
        }

        public string PhotoUrlScaledThumbnail(int maxHeight)
        {  
            return FileResource.FileResourceUrlScaledThumbnail(maxHeight);
        }

        public List<string> GetAdditionalCssClasses()
        {
            return new List<string>();
        }
    }
}