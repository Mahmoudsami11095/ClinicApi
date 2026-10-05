 // Copyright (c) Project Authors. All rights reserved.
 // Licensed under the standard project license.
 
 // Copyright (c) Project Authors. All rights reserved.
 // Licensed under the standard project license.
 
namespace Clinic.Application.Interfaces;

public interface IMaterialSeedingService
{
    Task SeedDefaultMaterialsAsync(string clinicId, string doctorId);
}
