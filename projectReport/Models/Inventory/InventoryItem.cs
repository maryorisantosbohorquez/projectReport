using System;

namespace ProjectReport.Models.Inventory
{
    public class InventoryItem
    {
        public string ItemCode { get; set; }          // Código interno: PAC-R-BOX, BARITE-SK, etc.
        public string Name { get; set; }              // Nombre comercial
        public string Category { get; set; }          // Mud Chemical, Cementing Chemical, Completion Chemical, General Chemical

        public string Packaging { get; set; }         // Caja, Bolsa, Estiba, Tambor, IBC, etc.
        public string Unit { get; set; }              // box, sk, drum, ibc, pallet

        public int QuantityAvailable { get; set; }    // Cantidad en esa unidad
        public int MinStock { get; set; }             // Stock mínimo recomendado
        public int MaxStock { get; set; }             // Stock máximo esperado

        public string Location { get; set; }          // Bodega / rack / patio
        public string Status { get; set; }            // Available, Low, Critical, Blocked

        public string HazardClass { get; set; }       // Non-Hazardous, Class 3, Class 8, etc.
        public string Supplier { get; set; }          // Proveedor / compañía de servicios
        public string BatchNumber { get; set; }       // Lote
        public DateTime? ExpirationDate { get; set; } // Vencimiento (si aplica)

        public DateTime? LastMovementDate { get; set; } // Último movimiento de inventario
    }
}
