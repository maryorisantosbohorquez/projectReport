using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProjectReport.Models.Inventory;

namespace ProjectReport.ViewModels.Inventory
{
    public class InventoryViewModel : INotifyPropertyChanged
    {
        private InventoryItem _selectedItem;
        private string _searchText;

        public ObservableCollection<InventoryItem> Items { get; } 
            = new ObservableCollection<InventoryItem>();

        public InventoryItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    // Aquí luego puedes implementar filtro si quieres
                }
            }
        }

        public InventoryViewModel()
        {
            SeedChemicals();
        }

        private void SeedChemicals()
        {
            var today = DateTime.Today;

            // ====== MUD CHEMICALS ======
            Items.Add(new InventoryItem
            {
                ItemCode = "BENT-SK",
                Name = "Bentonite 50 kg",
                Category = "Mud Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 180,
                MinStock = 100,
                MaxStock = 300,
                Location = "WH-A / Rack 1",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Local Supplier",
                BatchNumber = "BENT-23-091",
                ExpirationDate = today.AddYears(2),
                LastMovementDate = today.AddDays(-3)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "BARITE-SK",
                Name = "Barite 100 lb Sack",
                Category = "Mud Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 260,
                MinStock = 150,
                MaxStock = 400,
                Location = "WH-A / Rack 2",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Halliburton",
                BatchNumber = "BAR-23-044",
                ExpirationDate = today.AddYears(3),
                LastMovementDate = today.AddDays(-1)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "PAC-R-BOX",
                Name = "PAC Regular",
                Category = "Mud Chemical",
                Packaging = "Caja",
                Unit = "box",
                QuantityAvailable = 32,
                MinStock = 15,
                MaxStock = 60,
                Location = "WH-A / Shelf 3",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "MI-Swaco",
                BatchNumber = "PACR-23-017",
                ExpirationDate = today.AddYears(2),
                LastMovementDate = today.AddDays(-5)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "PAC-L-BOX",
                Name = "PAC Low Viscosity",
                Category = "Mud Chemical",
                Packaging = "Caja",
                Unit = "box",
                QuantityAvailable = 18,
                MinStock = 10,
                MaxStock = 40,
                Location = "WH-A / Shelf 3",
                Status = "Low",
                HazardClass = "Non-Hazardous",
                Supplier = "MI-Swaco",
                BatchNumber = "PACL-23-011",
                ExpirationDate = today.AddYears(2),
                LastMovementDate = today.AddDays(-7)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "XCD-BOX",
                Name = "XCD Polymer",
                Category = "Mud Chemical",
                Packaging = "Caja",
                Unit = "box",
                QuantityAvailable = 10,
                MinStock = 8,
                MaxStock = 25,
                Location = "WH-A / Shelf 4",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Baker Hughes",
                BatchNumber = "XCD-23-005",
                ExpirationDate = today.AddYears(2),
                LastMovementDate = today.AddDays(-10)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "CAUSTIC-SK",
                Name = "Caustic Soda 25 kg",
                Category = "Mud Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 40,
                MinStock = 25,
                MaxStock = 80,
                Location = "WH-B / Chem Area 1",
                Status = "Available",
                HazardClass = "Class 8",
                Supplier = "Generic Chemical",
                BatchNumber = "CAUS-23-021",
                ExpirationDate = today.AddYears(1),
                LastMovementDate = today.AddDays(-4)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "LUBE-DRUM",
                Name = "Liquid Lubricant",
                Category = "Mud Chemical",
                Packaging = "Tambor 55 gal",
                Unit = "drum",
                QuantityAvailable = 8,
                MinStock = 5,
                MaxStock = 15,
                Location = "WH-B / Drum Area",
                Status = "Available",
                HazardClass = "Class 3",
                Supplier = "Halliburton",
                BatchNumber = "LUBE-23-004",
                ExpirationDate = today.AddYears(1),
                LastMovementDate = today.AddDays(-12)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "CACO3-F-SK",
                Name = "Calcium Carbonate Fine",
                Category = "Mud Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 70,
                MinStock = 30,
                MaxStock = 120,
                Location = "WH-A / Rack 4",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Local Supplier",
                BatchNumber = "CACO3F-23-013",
                ExpirationDate = today.AddYears(3),
                LastMovementDate = today.AddDays(-8)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "CACO3-C-SK",
                Name = "Calcium Carbonate Coarse",
                Category = "Mud Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 50,
                MinStock = 25,
                MaxStock = 100,
                Location = "WH-A / Rack 4",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Local Supplier",
                BatchNumber = "CACO3C-23-007",
                ExpirationDate = today.AddYears(3),
                LastMovementDate = today.AddDays(-9)
            });

            // ====== CEMENTING CHEMICALS ======
            Items.Add(new InventoryItem
            {
                ItemCode = "CMT-G-SK",
                Name = "Cement Class G 94 lb",
                Category = "Cementing Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 240,
                MinStock = 150,
                MaxStock = 400,
                Location = "Yard / Cement Silo Area",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Lafarge",
                BatchNumber = "CMTG-23-066",
                ExpirationDate = today.AddYears(1),
                LastMovementDate = today.AddDays(-2)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "SILICA-SK",
                Name = "Silica Flour",
                Category = "Cementing Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 80,
                MinStock = 40,
                MaxStock = 150,
                Location = "WH-B / Rack 3",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Halliburton",
                BatchNumber = "SIL-23-019",
                ExpirationDate = today.AddYears(2),
                LastMovementDate = today.AddDays(-6)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "RET-LIQ-DRUM",
                Name = "Cement Retarder",
                Category = "Cementing Chemical",
                Packaging = "Tambor 55 gal",
                Unit = "drum",
                QuantityAvailable = 4,
                MinStock = 3,
                MaxStock = 8,
                Location = "WH-B / Chem Area 2",
                Status = "Low",
                HazardClass = "Class 8",
                Supplier = "Baker Hughes",
                BatchNumber = "RET-23-003",
                ExpirationDate = today.AddMonths(18),
                LastMovementDate = today.AddDays(-20)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "DISP-LIQ-DRUM",
                Name = "Cement Dispersant",
                Category = "Cementing Chemical",
                Packaging = "Tambor 55 gal",
                Unit = "drum",
                QuantityAvailable = 6,
                MinStock = 4,
                MaxStock = 10,
                Location = "WH-B / Chem Area 2",
                Status = "Available",
                HazardClass = "Class 8",
                Supplier = "Baker Hughes",
                BatchNumber = "DISP-23-004",
                ExpirationDate = today.AddMonths(18),
                LastMovementDate = today.AddDays(-15)
            });

            // ====== COMPLETION / STIMULATION ======
            Items.Add(new InventoryItem
            {
                ItemCode = "FR-IBC",
                Name = "Friction Reducer",
                Category = "Completion Chemical",
                Packaging = "IBC 1000 L",
                Unit = "ibc",
                QuantityAvailable = 3,
                MinStock = 2,
                MaxStock = 5,
                Location = "Yard / Fluid Area",
                Status = "Available",
                HazardClass = "Class 3",
                Supplier = "Service Company",
                BatchNumber = "FR-23-002",
                ExpirationDate = today.AddYears(1),
                LastMovementDate = today.AddDays(-11)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "BIOCIDE-DRUM",
                Name = "Biocide",
                Category = "Completion Chemical",
                Packaging = "Tambor 55 gal",
                Unit = "drum",
                QuantityAvailable = 5,
                MinStock = 3,
                MaxStock = 10,
                Location = "WH-B / Chem Area 3",
                Status = "Available",
                HazardClass = "Class 6",
                Supplier = "Service Company",
                BatchNumber = "BIO-23-009",
                ExpirationDate = today.AddMonths(12),
                LastMovementDate = today.AddDays(-18)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "CORR-INH-DRUM",
                Name = "Corrosion Inhibitor",
                Category = "Completion Chemical",
                Packaging = "Tambor 55 gal",
                Unit = "drum",
                QuantityAvailable = 4,
                MinStock = 3,
                MaxStock = 8,
                Location = "WH-B / Chem Area 3",
                Status = "Low",
                HazardClass = "Class 8",
                Supplier = "Service Company",
                BatchNumber = "CORR-23-006",
                ExpirationDate = today.AddMonths(10),
                LastMovementDate = today.AddDays(-25)
            });

            // ====== GENERAL FLUIDS ======
            Items.Add(new InventoryItem
            {
                ItemCode = "DIESEL-IBC",
                Name = "Diesel",
                Category = "General Chemical",
                Packaging = "IBC 1000 L",
                Unit = "ibc",
                QuantityAvailable = 6,
                MinStock = 4,
                MaxStock = 10,
                Location = "Tank Farm",
                Status = "Available",
                HazardClass = "Class 3",
                Supplier = "Fuel Supplier",
                BatchNumber = "DIE-23-031",
                ExpirationDate = null,
                LastMovementDate = today.AddDays(-1)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "BASE-OIL-IBC",
                Name = "Base Oil",
                Category = "General Chemical",
                Packaging = "IBC 1000 L",
                Unit = "ibc",
                QuantityAvailable = 4,
                MinStock = 3,
                MaxStock = 8,
                Location = "Tank Farm",
                Status = "Available",
                HazardClass = "Class 3",
                Supplier = "Mud Company",
                BatchNumber = "BO-23-014",
                ExpirationDate = null,
                LastMovementDate = today.AddDays(-3)
            });

            Items.Add(new InventoryItem
            {
                ItemCode = "KCL-SK",
                Name = "Potassium Chloride (KCl)",
                Category = "General Chemical",
                Packaging = "Bolsa",
                Unit = "sk",
                QuantityAvailable = 90,
                MinStock = 50,
                MaxStock = 150,
                Location = "WH-B / Rack 5",
                Status = "Available",
                HazardClass = "Non-Hazardous",
                Supplier = "Generic Chemical",
                BatchNumber = "KCL-23-022",
                ExpirationDate = today.AddYears(3),
                LastMovementDate = today.AddDays(-14)
            });
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
