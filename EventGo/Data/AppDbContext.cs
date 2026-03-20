<<<<<<< HEAD
﻿using Microsoft.EntityFrameworkCore;
using EventGo.Models;

namespace EventGo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Organizador> Organizadores { get; set; }
        public DbSet<Participante> Participantes { get; set; }
        public DbSet<Inscricao> Inscricoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relação 1:N: Organizador -> Eventos
            modelBuilder.Entity<Evento>()
                .HasOne(e => e.Organizador)
                .WithMany(o => o.Eventos)
                .HasForeignKey(e => e.OrganizadorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relação N:N via Inscricao: Evento <-> Participante
            modelBuilder.Entity<Inscricao>()
                .HasOne(i => i.Evento)
                .WithMany(e => e.Inscricoes)
                .HasForeignKey(i => i.EventoId);

            modelBuilder.Entity<Inscricao>()
                .HasOne(i => i.Participante)
                .WithMany(p => p.Inscricoes)
                .HasForeignKey(i => i.ParticipanteId);
        }
    }
}
=======
﻿namespace EventGo.Data
{
    public class AppDbContext
    {
    }
}
>>>>>>> 7bd6faf (models e dbcontext)
