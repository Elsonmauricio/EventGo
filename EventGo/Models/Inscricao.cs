<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> 333c2090e953223d18414127eea4a5c75613f002
﻿using System.ComponentModel.DataAnnotations;

namespace EventGo.Models
{
    public class Inscricao
    {
        public int Id { get; set; }

        public int EventoId { get; set; }
        public Evento? Evento { get; set; }

        public int ParticipanteId { get; set; }
        public Participante? Participante { get; set; }

        public DateTime DataInscricao { get; set; } = DateTime.Now;
    }
<<<<<<< HEAD
}
=======
﻿namespace EventGo.Models
{
    public class Inscricao
    {
    }
}
>>>>>>> 7bd6faf (models e dbcontext)
=======
}
>>>>>>> 333c2090e953223d18414127eea4a5c75613f002
