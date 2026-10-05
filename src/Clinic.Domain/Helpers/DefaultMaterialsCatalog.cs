/* Copyright (c) 2026. All rights reserved. */
/* Copyright (c) 2026. All rights reserved. */
namespace Clinic.Domain.Helpers;

using Clinic.Domain.Entities;

public static class DefaultMaterialsCatalog
{
    /// <summary>
    /// Generates a list of default materials for a given clinic and doctor.
    /// </summary>
    /// <param name="clinicId">The target clinic identifier.</param>
    /// <param name="doctorId">The target doctor identifier.</param>
    /// <returns>A list of initialized default Material entities.</returns>
    /// <summary>
    /// Generates a list of default materials for a given clinic and doctor.
    /// </summary>
    /// <param name="clinicId">The target clinic identifier.</param>
    /// <param name="doctorId">The target doctor identifier.</param>
    /// <returns>A list of initialized default Material entities.</returns>
    public static List<Material> GetDefaultMaterials(string clinicId, string doctorId)
    {
        return new List<Material>
        {
            // 🦷 Impression Materials
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Sili Kit BMS (Alginate)",
                Category = "Impression",
                Quantity = 0,
                Unit = "Kits",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 730.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Scope Alginate Scoop and Cup",
                Category = "Impression",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 24.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Addition Silicone (Kromopan PVS)",
                Category = "Impression",
                Quantity = 0,
                Unit = "Kits",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 1455.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Zhermack Light Silicone 140ml",
                Category = "Impression",
                Quantity = 0,
                Unit = "Tubes",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 560.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Impression Tray (Stainless Steel Set)",
                Category = "Impression",
                Quantity = 0,
                Unit = "Sets",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 9.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Impression Tray (Stainless Steel Kit)",
                Category = "Impression",
                Quantity = 0,
                Unit = "Kits",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 850.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Impression Plastic Tray (Medium)",
                Category = "Impression",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 20,
                SupplierName = "Dr MAHDY",
                UnitCost = 15.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Dental Aluminium Sectional Trays",
                Category = "Impression",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 10,
                SupplierName = "Dr MAHDY",
                UnitCost = 9.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Metal Spatula (Alginate Mixing)",
                Category = "Impression",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 20.00m,
                IsDefault = true
            },

            // 🔧 Restorative Materials
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Alpha Composite (Tokuyama Estelite)",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Syringes",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 190.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Prevest Orafil G Temporary Filling 40g",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Jars",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 192.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Charm Core (Temp Crown and Bridge 50ml)",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Cartridges",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 1400.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Han Temporary Crown Material 50ml",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Cartridges",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 1285.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Fibre Post Kit",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Kits",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 99.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Glass Slabs",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 5.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Gypsum Cast",
                Category = "Restorative",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 10,
                SupplierName = "Dr MAHDY",
                UnitCost = 10.00m,
                IsDefault = true
            },

            // 📐 Matrix Systems
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Celluloid Strips (Egypt)",
                Category = "Matrix",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 10,
                SupplierName = "Dr MAHDY",
                UnitCost = 11.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Celluloid Strips (Tor M)",
                Category = "Matrix",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 65.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Sectional Matrices (Various Sizes)",
                Category = "Matrix",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 145.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Saddle Matrix",
                Category = "Matrix",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 245.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Twin Sectional Matrix",
                Category = "Matrix",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 50.00m,
                IsDefault = true
            },

            // 🔬 Endodontic Materials
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Meta Biomed ADSEAL Resin Sealer 13.5g",
                Category = "Endodontic",
                Quantity = 0,
                Unit = "Tubes",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 795.00m,
                IsDefault = true
            },

            // 🛡️ Isolation and Protection
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Rubber Dam Sheets (Flamingo)",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Boxes",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 325.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Metal Frame Rubber Dam",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 35.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "R.D Punch (Rubber Dam Punch)",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 390.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Rubber Dam Clamp Forceps",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 280.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Liquid Rubber Dam (Light-cured)",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Syringes",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 150.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Cheek Retractor",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 35.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Teflon Tape (PTFE)",
                Category = "Isolation",
                Quantity = 0,
                Unit = "Rolls",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 14.00m,
                IsDefault = true
            },

            // ✂️ Hand Instruments
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Excavators",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 25.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Tweezer (Dressing Pliers)",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 50.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Probe (Periodontal)",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 25.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Scalpel Handle",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 25.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Scissor (Dental)",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 45.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Wax Knife",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 25.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Wax Carver",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 25.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Cutting Pliers",
                Category = "Instruments",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 90.00m,
                IsDefault = true
            },

            // 💉 Anesthesia
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Aspirating Dental Syringe (2-ring)",
                Category = "Anesthesia",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 190.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Dental Anesthetic Needles",
                Category = "Anesthesia",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 50,
                SupplierName = "Dr MAHDY",
                UnitCost = 2.00m,
                IsDefault = true
            },

            // 🧹 Finishing and Polishing
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Finishing Discs Kit",
                Category = "Finishing",
                Quantity = 0,
                Unit = "Kits",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 345.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Finishing Strips",
                Category = "Finishing",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 130.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Rag Wheel (Polishing)",
                Category = "Finishing",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 15.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Low Speed Round Stone",
                Category = "Finishing",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 18.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Mounted Stone (Flame Shape)",
                Category = "Finishing",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 15.00m,
                IsDefault = true
            },

            // 🧪 Lab and Wax Materials
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Dental Articulator",
                Category = "Lab",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 400.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Base Plate Wax (DENTAX)",
                Category = "Lab",
                Quantity = 0,
                Unit = "Boxes",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 205.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "DENTAX Blue Carving Wax",
                Category = "Lab",
                Quantity = 0,
                Unit = "Boxes",
                MinStockAlert = 2,
                SupplierName = "Dr MAHDY",
                UnitCost = 24.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Sticky Wax",
                Category = "Lab",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 7.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Artificial Teeth (Bana)",
                Category = "Lab",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 10,
                SupplierName = "Dr MAHDY",
                UnitCost = 1.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Steel Wire (0.7mm)",
                Category = "Lab",
                Quantity = 0,
                Unit = "Rolls",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 8.00m,
                IsDefault = true
            },

            // 📋 Burs and Accessories
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "ROTEX Bur Holder (128 piece)",
                Category = "Burs",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 1,
                SupplierName = "Dr MAHDY",
                UnitCost = 260.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Bur Converter",
                Category = "Burs",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 26.00m,
                IsDefault = true
            },

            // 🧤 Disposables and PPE
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Non-Woven Sponge (Gauze Pads)",
                Category = "Disposables",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 10,
                SupplierName = "Dr MAHDY",
                UnitCost = 32.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Disposable Trays",
                Category = "Disposables",
                Quantity = 0,
                Unit = "Packs",
                MinStockAlert = 20,
                SupplierName = "Dr MAHDY",
                UnitCost = 5.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Surgical Mask with Ties",
                Category = "Disposables",
                Quantity = 0,
                Unit = "Boxes",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 65.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Face Shield",
                Category = "Disposables",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 55.00m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Sterilization Pouch (60x130mm)",
                Category = "Disposables",
                Quantity = 0,
                Unit = "Pieces",
                MinStockAlert = 50,
                SupplierName = "Dr MAHDY",
                UnitCost = 0.50m,
                IsDefault = true
            },
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Vaseline (Petroleum Jelly)",
                Category = "Disposables",
                Quantity = 0,
                Unit = "Jars",
                MinStockAlert = 3,
                SupplierName = "Dr MAHDY",
                UnitCost = 15.00m,
                IsDefault = true
            },

            // 📝 Diagnostic
            new Material
            {
                Id = Guid.NewGuid().ToString(),
                ClinicId = clinicId,
                DoctorId = doctorId,
                Name = "Articulating Paper",
                Category = "Diagnostic",
                Quantity = 0,
                Unit = "Boxes",
                MinStockAlert = 5,
                SupplierName = "Dr MAHDY",
                UnitCost = 14.00m,
                IsDefault = true
            }
        };
    }
}
