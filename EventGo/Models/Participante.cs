using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventGo.Models
{
    public class Participante
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = "";

        public string Email { get; set; } = "";

        // Relações
        public List<Inscricao>? Inscricoes { get; set; }
    }
}