using System.ComponentModel.DataAnnotations;

namespace AscensoresIruna.Api.DTOs;

public class UpdateReportDto
{
    [Required(ErrorMessage = "El campo 'status' es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El campo 'status' no puede superar los 20 caracteres.")]
    [RegularExpression("^(Operativo|NoOperativo)$", ErrorMessage = "Solo puedes reportar los estados 'Operativo' o 'NoOperativo'.")]
    public string Status { get; set; } = string.Empty;
}