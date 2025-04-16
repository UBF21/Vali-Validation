// See https://aka.ms/new-console-template for more information

using Vali_Validation_Test.Models;
using Vali_Validation_Test.Validations;

var user = new UserDto { Name = "ASS", Email = "correo-invalido",Age = 0};
var validator = new UserDtoValidator();
var result = validator.Validate(user);

if (!result.IsValid)
{
    Console.WriteLine("Errores de validación:");
    foreach (var entry in result.Errors)
    {
        Console.WriteLine($"- {entry.Key}:");
        foreach (var message in entry.Value)
        {
            Console.WriteLine($"   • {message}");
        }
    }
}
else
{
    Console.WriteLine("✅ ¡Validación exitosa!");
}