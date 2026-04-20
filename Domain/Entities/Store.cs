using System.ComponentModel.DataAnnotations.Schema;

namespace Mart.Domain.Entities
{
    public class Store
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int ServiceRadiusInKm { get; set; } = 5; 
        public bool IsActive { get; set; } = true;


        [NotMapped]
        public double DistanceInKm { get; set; }
    }
}
