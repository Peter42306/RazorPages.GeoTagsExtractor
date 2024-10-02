using MetadataExtractor.Formats.Exif;
using MetadataExtractor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPages.GeoTagsExtractor.Models;

namespace RazorPages.GeoTagsExtractor.Pages
{
    public class GeoTagsExtractorModel : PageModel
    {
        public IEnumerable<FileModel> FilesCollection { get; set; } = default!;

        private readonly ApplicationContext _context;
        private readonly IWebHostEnvironment _environment;

        public GeoTagsExtractorModel(ApplicationContext context,IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task OnGetAsync()
        {            
                FilesCollection=await _context.Files.ToListAsync();            
        }

        [BindProperty]
        public IFormFile UploadedFile { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile != null) 
            {                

                string path="/Files/"+UploadedFile.FileName;

                using (var fileStream=new FileStream(_environment.WebRootPath+path,FileMode.Create))
                {
                    await UploadedFile.CopyToAsync(fileStream);
                }

                GeoTags geoTags = ExtractGeoTags(_environment.WebRootPath+path);

                FileModel newFile = new FileModel
                {
                    Name = UploadedFile.FileName,
                    Path = path,
                    Latitude = geoTags.Latitude,
                    Longitude = geoTags.Longitude
                };

                _context.Files.Add(newFile);
                await _context.SaveChangesAsync();

                return RedirectToPage("./GeoTagsExtractor");
            }

            return Page();
        }




        // Метод для извлечения геолокации
        private GeoTags ExtractGeoTags(string filePath)
        {
            GeoTags geoTags = new GeoTags();

            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath); // Читаем метаданные файла
                var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault(); // Ищем EXIF-теги с геолокацией

                if (gpsDirectory!=null)
                {
                    var location = gpsDirectory.GetGeoLocation();

                    if (location!=null)
                    {
                        geoTags.Latitude = location.Latitude;
                        geoTags.Longitude = location.Longitude;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during extraction of geo tag: {ex.Message}");                
            }

            return geoTags;
        }

        //public void OnGet()
        //{
        //}
    }
}
