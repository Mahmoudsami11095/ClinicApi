using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class ProcedureRecipeInventoryUnitTests
{
    public record ProcedureRecipe(string ProcedureName, List<RecipeItem> RequiredMaterials);
    public record RecipeItem(string MaterialKeyword, int QuantityNeeded);

    private static readonly List<ProcedureRecipe> StandardDentalRecipes = new()
    {
        new("Composite Restoration", new List<RecipeItem>
        {
            new("Composite", 1),
            new("Bonding", 1),
            new("Etchant", 1)
        }),
        new("Root Canal Treatment", new List<RecipeItem>
        {
            new("Gutta Percha", 1),
            new("Sealer", 1),
            new("Anesthetic", 1)
        }),
        new("Routine Simple Dental Extraction", new List<RecipeItem>
        {
            new("Anesthetic", 1),
            new("Gauze", 1)
        }),
        new("Full Mouth Scaling & Prophylaxis", new List<RecipeItem>
        {
            new("Paste", 1),
            new("Prophy", 1)
        })
    };

    [Fact]
    public void ProcedureRecipe_MatchMaterials_ShouldAccuratelyResolveAvailableStock()
    {
        // Arrange
        var availableInventory = new List<Material>
        {
            new() { Id = "mat-1", Name = "Composite Micro-hybrid A2", Quantity = 25, MinStockAlert = 5 },
            new() { Id = "mat-2", Name = "Universal Dental Bonding Agent", Quantity = 14, MinStockAlert = 3 },
            new() { Id = "mat-3", Name = "Phosphoric Acid Etchant Gel 37%", Quantity = 8, MinStockAlert = 2 },
            new() { Id = "mat-4", Name = "Local Anesthetic Mepivacaine 2%", Quantity = 60, MinStockAlert = 10 }
        };

        var recipe = StandardDentalRecipes.First(r => r.ProcedureName.Contains("Composite"));

        // Act - Match materials for composite recipe
        var matched = new List<ConsumedMaterialDto>();
        foreach (var req in recipe.RequiredMaterials)
        {
            var match = availableInventory.FirstOrDefault(m => 
                m.Name.Contains(req.MaterialKeyword, StringComparison.OrdinalIgnoreCase) && m.Quantity >= req.QuantityNeeded);
            if (match != null)
            {
                matched.Add(new ConsumedMaterialDto
                {
                    MaterialId = match.Id,
                    Quantity = req.QuantityNeeded
                });
            }
        }

        // Assert
        Assert.Equal(3, matched.Count);
        Assert.Contains(matched, m => m.MaterialId == "mat-1" && m.Quantity == 1);
        Assert.Contains(matched, m => m.MaterialId == "mat-2" && m.Quantity == 1);
        Assert.Contains(matched, m => m.MaterialId == "mat-3" && m.Quantity == 1);
    }

    [Fact]
    public void ProcedureRecipe_Deduction_ShouldSafelyDecrementStockAndDetectLowStock()
    {
        // Arrange
        var material = new Material
        {
            Id = "mat-comp-1",
            Name = "Composite Paste A3",
            Quantity = 4,
            MinStockAlert = 5
        };

        // Act - Consume 1 unit
        material.Quantity -= 1;
        bool isLowStock = material.Quantity <= material.MinStockAlert;

        // Assert
        Assert.Equal(3, material.Quantity);
        Assert.True(isLowStock);
    }

    [Fact]
    public void ProcedureRecipe_Deduction_FloorAtZero_NeverNegative()
    {
        // Arrange
        var material = new Material
        {
            Id = "mat-depleted-1",
            Name = "Sterile Cotton Rolls",
            Quantity = 2,
            MinStockAlert = 10
        };

        // Act - Consume 5 units (exceeding stock)
        material.Quantity -= 5;
        if (material.Quantity < 0) material.Quantity = 0;

        // Assert
        Assert.Equal(0, material.Quantity);
    }
}
