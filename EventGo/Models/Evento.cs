using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventGo.Models
{
    public class Evento
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = "";

        public DateTime Data { get; set; }

        public string Local { get; set; } = "";

        public int Vagas { get; set; }

        // Relações
        public int OrganizadorId { get; set; }
        public Organizador? Organizador { get; set; }

        public List<Inscricao>? Inscricoes { get; set; }
        public int Capacidade { get; set; }  // número máximo de participantes
    }
}