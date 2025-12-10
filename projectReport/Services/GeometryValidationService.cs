using System;
using System.Collections.Generic;
using System.Linq;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Services
{
    public class GeometryValidationService
    {
        public enum ValidationSeverity
        {
            Error,      // 🔴 Bloquea guardado
            Warning     // 🟡 Permite guardar con confirmación
        }

        public class ValidationError
        {
            public string ComponentId { get; set; }
            public string ComponentName { get; set; }
            public string Message { get; set; }
            public ValidationSeverity Severity { get; set; }
        }

        public class ValidationResult
        {
            public List<ValidationError> Items { get; set; } = new List<ValidationError>();
            public bool HasCriticalErrors => Items.Any(x => x.Severity == ValidationSeverity.Error);
            public bool HasWarnings => Items.Any(x => x.Severity == ValidationSeverity.Warning);
            public bool IsValid => !HasCriticalErrors; 
        }

        public ValidationResult ValidateWellbore(IEnumerable<WellboreComponent> components, double totalWellboreMD)
        {
            var result = new ValidationResult();
            var list = components.OrderBy(c => c.TopMD).ToList();

            // F3: Número Mínimo de Secciones
            if (!list.Any())
            {
                result.Items.Add(new ValidationError 
                { 
                    ComponentId = "-", 
                    ComponentName = "General", 
                    Message = "Debe agregar al menos una sección al wellbore", 
                    Severity = ValidationSeverity.Error 
                });
                return result;
            }

            // F1: IDs Únicos
            var duplicateIds = list.GroupBy(x => x.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateIds.Any())
            {
                 // F1 Error
                 foreach(var id in duplicateIds)
                    result.Items.Add(new ValidationError { ComponentId = "-", ComponentName = "General", Message = $"El ID {id} ya existe. Los IDs deben ser únicos", Severity = ValidationSeverity.Error });
            }

            // F2: Secuencia de IDs (Warning)
            // Solo verifica si los IDs son numéricos e incrementales para la advertencia
            bool idsAreSequential = true;
            for(int k=0; k<list.Count; k++)
            {
                int idVal = list[k].Id;
                if (idVal != k + 1) idsAreSequential = false;
            }
            if (!idsAreSequential)
            {
                result.Items.Add(new ValidationError { ComponentId = "-", ComponentName = "General", Message = "Los IDs no son secuenciales. Se recomienda mantener orden", Severity = ValidationSeverity.Warning });
            }

            // B5: Primera Sección Comienza en 0.00
            if (list[0].TopMD != 0)
            {
                result.Items.Add(new ValidationError { ComponentId = list[0].Id.ToString(), ComponentName = list[0].Name, Message = "La primera sección debe comenzar en 0.00 ft", Severity = ValidationSeverity.Error });
            }

            // B6: Última Sección Termina en Total MD (Warning)
            var last = list.Last();
            if (Math.Abs(last.BottomMD - totalWellboreMD) > 0.001)
            {
                 result.Items.Add(new ValidationError { ComponentId = last.Id.ToString(), ComponentName = last.Name, Message = $"La última sección termina en {last.BottomMD:F2} ft pero el Total Wellbore MD es {totalWellboreMD:F2} ft. ¿Es correcto?", Severity = ValidationSeverity.Warning });
            }

            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                var prev = i > 0 ? list[i - 1] : null;

                // --- CATEGORÍA A: VALIDACIONES DE DIÁMETROS ---

                // A5: OD No Puede Ser Cero
                // Para OpenHole: OD = diámetro del hoyo perforado
                // Para Casing/Liner: OD = diámetro exterior de la tubería
                if (cur.OD <= 0.001)
                {
                    string odMessage = cur.SectionType == WellboreSectionType.OpenHole 
                        ? "Error A5: OD cannot be 0.000. For OpenHole, enter the Hole Diameter (in)."
                        : "Error A5: OD cannot be 0.000. Enter the outer diameter of the pipe.";
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = odMessage, Severity = ValidationSeverity.Error });
                }

                // A3: Rangos Físicos Razonables - OD
                // Regla: 2.0 <= OD <= 60.0
                if ((cur.OD < 2.0 || cur.OD > 60.0) && cur.OD > 0.001)
                {
                    string msg = $"OD ({cur.OD:F3} in) está fuera del rango razonable (2.0 - 60.0 in). Verifique el valor ingresado";
                    if (cur.OD > 1000) msg += $"\n¿Quiso decir {cur.OD/1000:F3} in?";
                    
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = msg, Severity = ValidationSeverity.Error }); 
                    // User marked simple range as "Error" in A3 example text, but labeled "Detección de Errores de Entrada" as Error.
                    // Request says: "A3 ... Error: ..." so treating as Error.
                }

                // A6: ID No Puede Ser Cero
                // EXCEPCIÓN: OpenHole debe tener ID = 0 (no hay tubería interior)
                if (cur.ID <= 0.001 && cur.SectionType != WellboreSectionType.OpenHole)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "ID no puede ser 0.000 en secciones de tubería (Casing/Liner)", Severity = ValidationSeverity.Error });
                }
                
                // Validar que OpenHole SÍ tenga ID = 0
                if (cur.SectionType == WellboreSectionType.OpenHole && cur.ID > 0.001)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"OpenHole debe tener ID = 0.000 (no hay tubería interior). Valor actual: {cur.ID:F3} in", Severity = ValidationSeverity.Error });
                }

                // A4: Rangos Físicos Razonables - ID
                // Regla: 1.5 <= ID <= 55.0
                if ((cur.ID < 1.5 || cur.ID > 55.0) && cur.ID > 0.001)
                {
                     string msg = $"ID ({cur.ID:F3} in) está fuera del rango razonable (1.5 - 55.0 in). Verifique el valor ingresado";
                     if (cur.ID > 1000) msg += $"\n¿Quiso decir {cur.ID/1000:F3} in?";
                     result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = msg, Severity = ValidationSeverity.Error });
                }
                
                // A1: Internal Diameter Logic - ID must always be smaller than OD
                // Exception: OpenHole has ID = 0, so this check doesn't apply
                if (cur.SectionType != WellboreSectionType.OpenHole && cur.ID >= cur.OD && cur.OD > 0.001)
                {
                     result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "ID must always be smaller than OD", Severity = ValidationSeverity.Error });
                }

                // A2: Telescopic Diameter Rule - OD[n] < ID[n-1]
                // Applies to all rows except index 0
                if (prev != null && prev.ID > 0.001)
                {
                    if (cur.OD >= prev.ID)
                    {
                        result.Items.Add(new ValidationError 
                        { 
                            ComponentId = cur.Id.ToString(), 
                            ComponentName = cur.Name, 
                            Message = $"Error A2: El Diámetro Exterior (OD={cur.OD:F3} in) o Diámetro del Hoyo es mayor o igual que el Diámetro Interior (ID={prev.ID:F3} in) de la sección superior. El pozo no cumple la progresión telescópica.", 
                            Severity = ValidationSeverity.Error 
                        });
                    }
                }

                // --- CATEGORÍA B: VALIDACIONES DE PROFUNDIDAD ---

                // B1: Bottom > Top
                if (cur.BottomMD <= cur.TopMD)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Bottom MD ({cur.BottomMD:F2} ft) debe ser mayor que Top MD ({cur.TopMD:F2} ft)", Severity = ValidationSeverity.Error });
                }

                // B4: No Exceder Total Wellbore MD
                if (cur.BottomMD > totalWellboreMD + 0.001) // Tolerance
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Bottom MD ({cur.BottomMD:F2} ft) excede la profundidad total del pozo ({totalWellboreMD:F2} ft)", Severity = ValidationSeverity.Error });
                }

                if (prev != null)
                {
                    // B3: Solapamientos
                    // Rule: Top(n) >= Bottom(n-1)
                    if (cur.TopMD < prev.BottomMD)
                    {
                         result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Las secciones se solapan. La sección {cur.Id} comienza en {cur.TopMD:F2} ft pero la sección anterior termina en {prev.BottomMD:F2} ft", Severity = ValidationSeverity.Error });
                    }

                    // B2: Gaps
                    // Rule: Top(n) == Bottom(n-1)
                    // Allow small tolerance
                    if (cur.TopMD > prev.BottomMD + 0.01)
                    {
                        result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Existe un gap entre secciones. Top MD ({cur.TopMD:F2} ft) debe comenzar donde termina la sección anterior ({prev.BottomMD:F2} ft)", Severity = ValidationSeverity.Error });
                    }
                }

                // --- CATEGORÍA C: TIPO DE SECCIÓN ---

                // C1: Casing/Liner Depth Progression
                // Rule: For tubular sections (Casing/Liner), BottomMD[n] >= BottomMD[n-1]
                // Exception: Casing Override (same TopMD, deeper BottomMD)
                if (prev != null && 
                    (cur.SectionType == WellboreSectionType.Casing || cur.SectionType == WellboreSectionType.Liner) &&
                    (prev.SectionType == WellboreSectionType.Casing || prev.SectionType == WellboreSectionType.Liner))
                {
                    // Check for Casing Override: Same TopMD, deeper or equal BottomMD
                    bool isCasingOverride = Math.Abs(cur.TopMD - prev.TopMD) < 0.01 && cur.BottomMD >= prev.BottomMD;
                    
                    if (isCasingOverride)
                    {
                        // Valid override - show warning for user awareness (matches spec)
                        result.Items.Add(new ValidationError 
                        { 
                            ComponentId = cur.Id.ToString(), 
                            ComponentName = cur.Name, 
                            Message = "⚠ Casing Override detected → previous casing replaced.", 
                            Severity = ValidationSeverity.Warning 
                        });
                    }
                    else if (cur.BottomMD < prev.BottomMD)
                    {
                        // Invalid: Bottom MD decreased without being an override
                        result.Items.Add(new ValidationError 
                        { 
                            ComponentId = cur.Id.ToString(), 
                            ComponentName = cur.Name, 
                            Message = "Error D3: El Bottom MD de una sección de revestimiento anidada no puede ser menor que el Bottom MD de la sección superior inmediata.", 
                            Severity = ValidationSeverity.Error 
                        });
                    }
                }

                // C3/C4: OpenHole Washout Requerido (MANDATORY - Hard Error)
                if (cur.SectionType == WellboreSectionType.OpenHole)
                {
                    if (double.IsNaN(cur.Washout) || cur.Washout < 0)
                    {
                         result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Error C4: Washout is required for Open Hole volume calculation.", Severity = ValidationSeverity.Error });
                    }
                    else if (cur.Washout > 50)
                    {
                         result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Error C4: Washout value exceeds reasonable range (0-50%). Typical values: 5-25%", Severity = ValidationSeverity.Error });
                    }
                    else if (cur.Washout < 0.01)
                    {
                         result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "Validación C3: Mínimo W ≥ 0.01%. Advertencia: Washout de 0% en OpenHole es poco común. Valores típicos: 5-25%. ¿Es correcto?", Severity = ValidationSeverity.Warning });
                    }
                }

                // --- CATEGORÍA D: VOLUMEN ---
                
                // D1: Volumen Positivo
                if (cur.Volume <= 0)
                {
                     result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = "El volumen calculado debe ser mayor que 0 bbl", Severity = ValidationSeverity.Error });
                }
                
                // D4: Volume Error Check
                if (cur.Volume > 100000)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Volumen de {cur.Volume:F2} bbl indica errores graves en diámetros. Revise OD e ID", Severity = ValidationSeverity.Error });
                }
                // D2: Volumen Razonable (Warning)
                else if (cur.Volume > 10000)
                {
                    result.Items.Add(new ValidationError { ComponentId = cur.Id.ToString(), ComponentName = cur.Name, Message = $"Volumen de {cur.Volume:F2} bbl parece excesivo. Verifique los diámetros ingresados", Severity = ValidationSeverity.Warning });
                }
            }

            return result;
        }
    }
}
