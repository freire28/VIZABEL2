using Microsoft.EntityFrameworkCore;
using GestaoPedidosVizabel.Models;

namespace GestaoPedidosVizabel.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Configuracao> Configuracoes { get; set; }
        public DbSet<TamanhoProduto> TamanhoProdutos { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<GradeTamanho> GradeTamanhos { get; set; }
        public DbSet<FormaPagamento> FormasPagamento { get; set; }
        public DbSet<EtapaProducao> EtapasProducao { get; set; }
        public DbSet<Funcao> Funcoes { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<FuncionarioFuncao> FuncionarioFuncoes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<ProdutoEtapaProducao> ProdutoEtapasProducao { get; set; }
        public DbSet<ProdutoGrade> ProdutoGrades { get; set; }
        public DbSet<StatusPedido> StatusPedidos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoProduto> PedidoProdutos { get; set; }
        public DbSet<PedidoProdTamanho> PedidoProdTamanhos { get; set; }
        public DbSet<PedidoImagem> PedidoImagens { get; set; }
        public DbSet<ProdutoPedidoEtapaProd> ProdutoPedidoEtapaProds { get; set; }
        public DbSet<NFe> NFes { get; set; }
        public DbSet<NFeDestinatario> NFeDestinatarios { get; set; }
        public DbSet<NFeItem> NFeItens { get; set; }
        public DbSet<NFeItemImposto> NFeItemImpostos { get; set; }
        public DbSet<NFePagamento> NFePagamentos { get; set; }
        public DbSet<NFeNaturezaOperacao> NFeNaturezaOperacoes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Permissao> Permissoes { get; set; }
        public DbSet<UsuarioPermissao> UsuarioPermissoes { get; set; }
        public DbSet<TabelaIpt> TabelaIpt { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.IdCliente);
                entity.ToTable("CLIENTES");
                entity.Property(e => e.IdCliente).HasColumnName("ID_CLIENTE");
                entity.Property(e => e.Nomerazao).HasColumnName("NOMERAZAO").HasMaxLength(250);
                entity.Property(e => e.TipoPessoa).HasColumnName("TIPO_PESSOA");
                entity.Property(e => e.Fantasia).HasColumnName("FANTASIA").HasMaxLength(60);
                entity.Property(e => e.Cpfcnpj).HasColumnName("CPFCNPJ").HasMaxLength(20);
                entity.Property(e => e.RgIe).HasColumnName("RG_IE").HasMaxLength(60);
                entity.Property(e => e.EnderecoLogradouro).HasColumnName("ENDERECO_LOGRADOURO").HasMaxLength(255);
                entity.Property(e => e.EnderecoNumero).HasColumnName("ENDERECO_NUMERO").HasMaxLength(10);
                entity.Property(e => e.EnderecoComplemento).HasColumnName("ENDERECO_COMPLEMENTO").HasMaxLength(60);
                entity.Property(e => e.EnderecoBairro).HasColumnName("ENDERECO_BAIRRO").HasMaxLength(60);
                entity.Property(e => e.EnderecoCidade).HasColumnName("ENDERECO_CIDADE").HasMaxLength(60);
                entity.Property(e => e.EnderecoUf).HasColumnName("ENDERECO_UF").HasMaxLength(2);
                entity.Property(e => e.EnderecoCep).HasColumnName("ENDERECO_CEP").HasMaxLength(10);
                entity.Property(e => e.Email).HasColumnName("EMAIL").HasMaxLength(200);
                entity.Property(e => e.Fone).HasColumnName("FONE").HasMaxLength(20);
                entity.Property(e => e.Whatsapp).HasColumnName("WHATSAPP").HasMaxLength(20);
                entity.Property(e => e.Contato).HasColumnName("CONTATO").HasMaxLength(60);
                entity.Property(e => e.FoneContato).HasColumnName("FONE_CONTATO").HasMaxLength(20);
                entity.Property(e => e.CodCliente).HasColumnName("COD_CLIENTE");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<Configuracao>(entity =>
            {
                entity.HasKey(e => e.IdConfiguracao);
                entity.ToTable("CONFIGURACOES");
                entity.Property(e => e.IdConfiguracao).HasColumnName("ID_CONFIGURACAO");
                entity.Property(e => e.Chave).HasColumnName("CHAVE").HasMaxLength(60);
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(255);
                entity.Property(e => e.Valor).HasColumnName("VALOR").HasMaxLength(60);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                entity.Property(e => e.ConsiderarNoPrazoEntrega).HasColumnName("CONSIDERAR_NO_PRAZO_ENTREGA");
            });

            modelBuilder.Entity<TamanhoProduto>(entity =>
            {
                entity.HasKey(e => e.IdTamanhoproduto);
                entity.ToTable("TAMANHOS_PRODUTOS");
                entity.Property(e => e.IdTamanhoproduto).HasColumnName("ID_TAMANHOPRODUTO");
                entity.Property(e => e.Tamanho).HasColumnName("TAMANHO").HasMaxLength(60);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.IdGrade).HasName("PK_GRADE_TAMANHOS");
                entity.ToTable("GRADES");
                entity.Property(e => e.IdGrade).HasColumnName("ID_GRADE");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(60);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<GradeTamanho>(entity =>
            {
                entity.HasKey(e => e.IdGradeTamanho).HasName("PK_GRADE_TAMANHOS_1");
                entity.ToTable("GRADE_TAMANHOS");
                entity.Property(e => e.IdGradeTamanho).HasColumnName("ID_GRADE_TAMANHO");
                entity.Property(e => e.IdGrade).HasColumnName("ID_GRADE");
                entity.Property(e => e.IdTamanho).HasColumnName("ID_TAMANHO");
                
                entity.HasOne(d => d.Grade)
                    .WithMany(p => p.GradeTamanhos)
                    .HasForeignKey(d => d.IdGrade)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.TamanhoProduto)
                    .WithMany(p => p.GradeTamanhos)
                    .HasForeignKey(d => d.IdTamanho)
                    .HasPrincipalKey(p => p.IdTamanhoproduto)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar duplicatas
                entity.HasIndex(e => new { e.IdGrade, e.IdTamanho })
                    .IsUnique()
                    .HasDatabaseName("IX_GRADE_TAMANHOS_UNIQUE");
            });

            modelBuilder.Entity<FormaPagamento>(entity =>
            {
                entity.HasKey(e => e.IdFormapagamento);
                entity.ToTable("FORMAS_PAGAMENTO");
                entity.Property(e => e.IdFormapagamento).HasColumnName("ID_FORMAPAGAMENTO");
                entity.Property(e => e.Codigo).HasColumnName("CODIGO").HasMaxLength(2);
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(10);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<EtapaProducao>(entity =>
            {
                entity.HasKey(e => e.IdEtapa);
                entity.ToTable("ETAPAS_PRODUCAO");
                entity.Property(e => e.IdEtapa).HasColumnName("ID_ETAPA");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(60);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                entity.Property(e => e.QuantidadeDias).HasColumnName("QUANTIDADE_DIAS");
                entity.Property(e => e.IdFuncao).HasColumnName("ID_FUNCAO");
            });

            modelBuilder.Entity<Funcao>(entity =>
            {
                entity.HasKey(e => e.IdFuncao);
                entity.ToTable("FUNCOES");
                entity.Property(e => e.IdFuncao).HasColumnName("ID_FUNCAO");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(60);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<Funcionario>(entity =>
            {
                entity.HasKey(e => e.IdFuncionario);
                entity.ToTable("FUNCIONARIOS");
                entity.Property(e => e.IdFuncionario).HasColumnName("ID_FUNCIONARIO");
                entity.Property(e => e.Nome).HasColumnName("NOME").HasMaxLength(60);
                entity.Property(e => e.Celular).HasColumnName("CELULAR").HasMaxLength(20);
                entity.Property(e => e.FuncaoPrincipal).HasColumnName("FUNCAO_PRINCIPAL");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                entity.Property(e => e.Vendedor).HasColumnName("VENDEDOR");
                entity.Property(e => e.Pin).HasColumnName("PIN").HasMaxLength(5);
            });

            modelBuilder.Entity<FuncionarioFuncao>(entity =>
            {
                entity.HasKey(e => e.IdFuncionarioFuncao);
                entity.ToTable("FUNCIONARIOS_FUNCOES");
                entity.Property(e => e.IdFuncionarioFuncao).HasColumnName("ID_FUNCIONARIO_FUNCAO");
                entity.Property(e => e.IdFuncionario).HasColumnName("ID_FUNCIONARIO");
                entity.Property(e => e.IdFuncao).HasColumnName("ID_FUNCAO");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                
                entity.HasOne(d => d.Funcionario)
                    .WithMany(p => p.FuncionarioFuncoes)
                    .HasForeignKey(d => d.IdFuncionario)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Funcao)
                    .WithMany(p => p.FuncionarioFuncoes)
                    .HasForeignKey(d => d.IdFuncao)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar duplicatas
                entity.HasIndex(e => new { e.IdFuncionario, e.IdFuncao })
                    .IsUnique()
                    .HasDatabaseName("IX_FUNCIONARIOS_FUNCOES_UNIQUE");
            });

            modelBuilder.Entity<Produto>(entity =>
            {
                entity.HasKey(e => e.IdProduto);
                entity.ToTable("PRODUTOS");
                entity.Property(e => e.IdProduto).HasColumnName("ID_PRODUTO");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(200);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                entity.Property(e => e.PrazoEntrega).HasColumnName("PRAZO_ENTREGA");
                entity.Property(e => e.FabricacaoTerceirizada).HasColumnName("FABRICACAO_TERCEIRIZADA");
                entity.Property(e => e.Ncmsh).HasColumnName("NCMSH").HasMaxLength(20);
                entity.Property(e => e.Csosn).HasColumnName("CSOSN").HasMaxLength(20);
                entity.Property(e => e.Cfop).HasColumnName("CFOP").HasMaxLength(20);
            });

            modelBuilder.Entity<ProdutoEtapaProducao>(entity =>
            {
                entity.HasKey(e => e.IdProdutoEtapa);
                entity.ToTable("PRODUTOS_ETAPAS_PRODUCAO");
                entity.Property(e => e.IdProdutoEtapa).HasColumnName("ID_PRODUTO_ETAPA");
                entity.Property(e => e.IdProduto).HasColumnName("ID_PRODUTO");
                entity.Property(e => e.IdEtapa).HasColumnName("ID_ETAPA");
                entity.Property(e => e.Sequencia).HasColumnName("SEQUENCIA");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                
                entity.HasOne(d => d.Produto)
                    .WithMany(p => p.ProdutoEtapasProducao)
                    .HasForeignKey(d => d.IdProduto)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.EtapaProducao)
                    .WithMany(p => p.ProdutoEtapasProducao)
                    .HasForeignKey(d => d.IdEtapa)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar duplicatas
                entity.HasIndex(e => new { e.IdProduto, e.IdEtapa })
                    .IsUnique()
                    .HasDatabaseName("IX_PRODUTOS_ETAPAS_PRODUCAO_UNIQUE");
            });

            modelBuilder.Entity<ProdutoGrade>(entity =>
            {
                entity.HasKey(e => e.IdProdutoGrade);
                entity.ToTable("PRODUTO_GRADES");
                entity.Property(e => e.IdProdutoGrade).HasColumnName("ID_PRODUTO_GRADE");
                entity.Property(e => e.IdProduto).HasColumnName("ID_PRODUTO");
                entity.Property(e => e.IdGrade).HasColumnName("ID_GRADE");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                
                entity.HasOne(d => d.Produto)
                    .WithMany(p => p.ProdutoGrades)
                    .HasForeignKey(d => d.IdProduto)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Grade)
                    .WithMany(p => p.ProdutoGrades)
                    .HasForeignKey(d => d.IdGrade)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar duplicatas
                entity.HasIndex(e => new { e.IdProduto, e.IdGrade })
                    .IsUnique()
                    .HasDatabaseName("IX_PRODUTO_GRADES_UNIQUE");
            });

            modelBuilder.Entity<StatusPedido>(entity =>
            {
                entity.HasKey(e => e.IdStatuspedido).HasName("PK_STATUS_PEDIDO_1");
                entity.ToTable("STATUS_PEDIDO");
                entity.Property(e => e.IdStatuspedido).HasColumnName("ID_STATUSPEDIDO");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(60);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(e => e.IdPedido);
                entity.ToTable("PEDIDOS");
                entity.Property(e => e.IdPedido).HasColumnName("ID_PEDIDO");
                entity.Property(e => e.CodPedido).HasColumnName("COD_PEDIDO");
                entity.Property(e => e.IdCliente).HasColumnName("ID_CLIENTE");
                entity.Property(e => e.DataPedido).HasColumnName("DATA_PEDIDO").HasColumnType("datetime");
                entity.Property(e => e.DataEntrega).HasColumnName("DATA_ENTREGA").HasColumnType("datetime");
                entity.Property(e => e.IdStatuspedido).HasColumnName("ID_STATUSPEDIDO");
                entity.Property(e => e.IdVendedor).HasColumnName("ID_VENDEDOR");
                entity.Property(e => e.IdFormapagamento).HasColumnName("ID_FORMAPAGAMENTO");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                entity.Property(e => e.Observacoes).HasColumnName("OBSERVACOES").HasMaxLength(1000);
                entity.Property(e => e.EmitirNfe).HasColumnName("EMITIR_NFE");
            });

            modelBuilder.Entity<PedidoProduto>(entity =>
            {
                entity.HasKey(e => e.IdPedidoproduto).HasName("PK_PEDIDO_PRODUTOS_1");
                entity.ToTable("PEDIDO_PRODUTOS");
                entity.Property(e => e.IdPedidoproduto).HasColumnName("ID_PEDIDOPRODUTO");
                entity.Property(e => e.IdPedido).HasColumnName("ID_PEDIDO");
                entity.Property(e => e.IdProduto).HasColumnName("ID_PRODUTO");
                entity.Property(e => e.IdGrade).HasColumnName("ID_GRADE");
                entity.Property(e => e.Quantidade).HasColumnName("QUANTIDADE");
                entity.Property(e => e.IdEtapaProducao).HasColumnName("ID_ETAPA_PRODUCAO");
                entity.Property(e => e.IdFuncionarioResponsavel).HasColumnName("ID_FUNCIONARIO_RESPONSAVEL");
                entity.Property(e => e.ValorVenda).HasColumnName("VALOR_VENDA").HasColumnType("numeric(18, 2)");
            });

            modelBuilder.Entity<PedidoProdTamanho>(entity =>
            {
                entity.HasKey(e => new { e.IdGradePedProd, e.IdPedidoproduto }).HasName("PK_PEDIDO_PROD_TAMANHOS_1");
                entity.ToTable("PEDIDO_PROD_TAMANHOS");
                entity.Property(e => e.IdGradePedProd)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("ID_GRADE_PED_PROD");
                entity.Property(e => e.IdPedidoproduto).HasColumnName("ID_PEDIDOPRODUTO");
                entity.Property(e => e.IdGradeTamanho).HasColumnName("ID_GRADE_TAMANHO");
                entity.Property(e => e.Quantidade).HasColumnName("QUANTIDADE");
                entity.Property(e => e.IdEtapa).HasColumnName("ID_ETAPA");
                
                entity.HasOne(d => d.PedidoProduto)
                    .WithMany(p => p.PedidoProdTamanhos)
                    .HasForeignKey(d => d.IdPedidoproduto)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PedidoImagem>(entity =>
            {
                entity.HasKey(e => e.IdImagem);
                entity.ToTable("PEDIDO_IMAGENS");
                entity.Property(e => e.IdImagem).HasColumnName("ID_IMAGEM");
                entity.Property(e => e.IdPedidoproduto).HasColumnName("ID_PEDIDOPRODUTO");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(60);
                entity.Property(e => e.Imagem).HasColumnName("IMAGEM");
                
                entity.HasOne(d => d.PedidoProduto)
                    .WithMany(p => p.PedidoImagens)
                    .HasForeignKey(d => d.IdPedidoproduto)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProdutoPedidoEtapaProd>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("PRODUTO_PEDIDO_ETAPA_PROD");
                entity.Property(e => e.Id).HasColumnName("ID").ValueGeneratedOnAdd();
                entity.Property(e => e.IdEtapaProducao).HasColumnName("ID_ETAPA_PRODUCAO");
                entity.Property(e => e.IdPedidoProduto).HasColumnName("ID_PEDIDO_PRODUTO");
                entity.Property(e => e.IdFuncionario).HasColumnName("ID_FUNCIONARIO");
                entity.Property(e => e.Concluido).HasColumnName("CONCLUIDO");
                entity.Property(e => e.IdGradePedProd).HasColumnName("ID_GRADE_PED_PROD");
                entity.Property(e => e.Quantidade).HasColumnName("QUANTIDADE");
                entity.Property(e => e.IdTamanho).HasColumnName("ID_TAMANHO");
                entity.Property(e => e.Reposicao).HasColumnName("REPOSICAO");
                
                entity.HasOne(d => d.EtapaProducao)
                    .WithMany()
                    .HasForeignKey(d => d.IdEtapaProducao)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(d => d.PedidoProduto)
                    .WithMany()
                    .HasForeignKey(d => d.IdPedidoProduto)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(d => d.Funcionario)
                    .WithMany()
                    .HasForeignKey(d => d.IdFuncionario)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.IdUsuario);
                entity.ToTable("USUARIOS");
                entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
                entity.Property(e => e.Login).HasColumnName("LOGIN").HasMaxLength(50);
                entity.Property(e => e.Senha).HasColumnName("SENHA").HasMaxLength(255);
                entity.Property(e => e.Nome).HasColumnName("NOME").HasMaxLength(100);
                entity.Property(e => e.Email).HasColumnName("EMAIL").HasMaxLength(200);
                entity.Property(e => e.Administrador).HasColumnName("ADMINISTRADOR");
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                entity.Property(e => e.DataCriacao).HasColumnName("DATA_CRIACAO").HasColumnType("datetime");
                
                // Índice único para login
                entity.HasIndex(e => e.Login)
                    .IsUnique()
                    .HasDatabaseName("IX_USUARIOS_LOGIN_UNIQUE");
            });

            modelBuilder.Entity<Permissao>(entity =>
            {
                entity.HasKey(e => e.IdPermissao);
                entity.ToTable("PERMISSOES");
                entity.Property(e => e.IdPermissao).HasColumnName("ID_PERMISSAO");
                entity.Property(e => e.Controller).HasColumnName("CONTROLLER").HasMaxLength(100);
                entity.Property(e => e.Acao).HasColumnName("ACAO").HasMaxLength(100);
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(200);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
                
                // Índice único para controller + ação
                entity.HasIndex(e => new { e.Controller, e.Acao })
                    .IsUnique()
                    .HasDatabaseName("IX_PERMISSOES_CONTROLLER_ACAO_UNIQUE");
            });

            modelBuilder.Entity<UsuarioPermissao>(entity =>
            {
                entity.HasKey(e => e.IdUsuarioPermissao);
                entity.ToTable("USUARIO_PERMISSOES");
                entity.Property(e => e.IdUsuarioPermissao).HasColumnName("ID_USUARIO_PERMISSAO");
                entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
                entity.Property(e => e.IdPermissao).HasColumnName("ID_PERMISSAO");
                entity.Property(e => e.PodeVisualizar).HasColumnName("PODE_VISUALIZAR");
                entity.Property(e => e.PodeIncluir).HasColumnName("PODE_INCLUIR");
                entity.Property(e => e.PodeAlterar).HasColumnName("PODE_ALTERAR");
                entity.Property(e => e.PodeExcluir).HasColumnName("PODE_EXCLUIR");
                
                entity.HasOne(d => d.Usuario)
                    .WithMany(p => p.UsuarioPermissoes)
                    .HasForeignKey(d => d.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Permissao)
                    .WithMany(p => p.UsuarioPermissoes)
                    .HasForeignKey(d => d.IdPermissao)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar duplicatas
                entity.HasIndex(e => new { e.IdUsuario, e.IdPermissao })
                    .IsUnique()
                    .HasDatabaseName("IX_USUARIO_PERMISSOES_UNIQUE");
            });

            modelBuilder.Entity<NFe>(entity =>
            {
                entity.HasKey(e => e.IdNfe);
                entity.ToTable("NFE");
                entity.Property(e => e.IdNfe).HasColumnName("ID_NFE");
                entity.Property(e => e.IdEmpresa).HasColumnName("ID_EMPRESA");
                entity.Property(e => e.Modelo).HasColumnName("MODELO").HasMaxLength(2).HasDefaultValue("55");
                entity.Property(e => e.Serie).HasColumnName("SERIE");
                entity.Property(e => e.Numero).HasColumnName("NUMERO");
                entity.Property(e => e.NaturezaOperacao).HasColumnName("NATUREZA_OPERACAO").HasMaxLength(100);
                entity.Property(e => e.DataEmissao).HasColumnName("DATA_EMISSAO").HasColumnType("datetime2");
                entity.Property(e => e.TipoNfe).HasColumnName("TIPO_NFE");
                entity.Property(e => e.Finalidade).HasColumnName("FINALIDADE");
                entity.Property(e => e.Ambiente).HasColumnName("AMBIENTE");
                entity.Property(e => e.ValorProdutos).HasColumnName("VALOR_PRODUTOS").HasColumnType("decimal(15,2)");
                entity.Property(e => e.ValorTotalNfe).HasColumnName("VALOR_TOTAL_NFE").HasColumnType("decimal(15,2)");
                entity.Property(e => e.Status).HasColumnName("STATUS").HasMaxLength(20);
                entity.Property(e => e.ChaveAcesso).HasColumnName("CHAVE_ACESSO").HasMaxLength(255);
                entity.Property(e => e.DataSaida).HasColumnName("DATA_SAIDA").HasColumnType("datetime");
                entity.Property(e => e.RegimeTributario).HasColumnName("REGIME_TRIBUTARIO").HasMaxLength(50);
                entity.Property(e => e.IdPedido).HasColumnName("ID_PEDIDO");
                
                entity.HasOne(d => d.Pedido)
                    .WithMany()
                    .HasForeignKey(d => d.IdPedido)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(d => d.Destinatario)
                    .WithOne(p => p.NFe)
                    .HasForeignKey<NFeDestinatario>(d => d.IdNfe)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NFeDestinatario>(entity =>
            {
                entity.HasKey(e => e.IdDestinatario);
                entity.ToTable("NFE_DESTINATARIO");
                entity.Property(e => e.IdDestinatario).HasColumnName("ID_DESTINATARIO");
                entity.Property(e => e.IdNfe).HasColumnName("ID_NFE");
                entity.Property(e => e.CnpjCpf).HasColumnName("CNPJ_CPF").HasMaxLength(14);
                entity.Property(e => e.Nome).HasColumnName("NOME").HasMaxLength(150);
                entity.Property(e => e.IndIeDest).HasColumnName("IND_IE_DEST");
                entity.Property(e => e.Ie).HasColumnName("IE").HasMaxLength(20);
                entity.Property(e => e.Logradouro).HasColumnName("LOGRADOURO").HasMaxLength(150);
                entity.Property(e => e.Numero).HasColumnName("NUMERO").HasMaxLength(10);
                entity.Property(e => e.Bairro).HasColumnName("BAIRRO").HasMaxLength(100);
                entity.Property(e => e.CodMun).HasColumnName("COD_MUN");
                entity.Property(e => e.Municipio).HasColumnName("MUNICIPIO").HasMaxLength(100);
                entity.Property(e => e.Uf).HasColumnName("UF").HasMaxLength(2);
                entity.Property(e => e.Cep).HasColumnName("CEP").HasMaxLength(8);
            });

            modelBuilder.Entity<NFeItem>(entity =>
            {
                entity.HasKey(e => e.IdItem);
                entity.ToTable("NFE_ITEM");
                entity.Property(e => e.IdItem).HasColumnName("ID_ITEM");
                entity.Property(e => e.IdNfe).HasColumnName("ID_NFE");
                entity.Property(e => e.CodProduto).HasColumnName("COD_PRODUTO").HasMaxLength(60);
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(200);
                entity.Property(e => e.Ncm).HasColumnName("NCM").HasMaxLength(8);
                entity.Property(e => e.Cfop).HasColumnName("CFOP").HasMaxLength(4);
                entity.Property(e => e.Unidade).HasColumnName("UNIDADE").HasMaxLength(10);
                entity.Property(e => e.Quantidade).HasColumnName("QUANTIDADE").HasColumnType("decimal(15,4)");
                entity.Property(e => e.ValorUnitario).HasColumnName("VALOR_UNITARIO").HasColumnType("decimal(15,4)");
                entity.Property(e => e.ValorTotal).HasColumnName("VALOR_TOTAL").HasColumnType("decimal(15,2)");
                
                entity.HasOne(d => d.NFe)
                    .WithMany(p => p.Itens)
                    .HasForeignKey(d => d.IdNfe)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NFeItemImposto>(entity =>
            {
                entity.HasKey(e => e.IdImposto);
                entity.ToTable("NFE_ITEM_IMPOSTO");
                entity.Property(e => e.IdImposto).HasColumnName("ID_IMPOSTO");
                entity.Property(e => e.IdItem).HasColumnName("ID_ITEM");
                entity.Property(e => e.Origem).HasColumnName("ORIGEM");
                entity.Property(e => e.CstCsosn).HasColumnName("CST_CSOSN").HasMaxLength(3);
                entity.Property(e => e.BaseIcms).HasColumnName("BASE_ICMS").HasColumnType("decimal(15,2)").HasDefaultValue(0);
                entity.Property(e => e.AliquotaIcms).HasColumnName("ALIQUOTA_ICMS").HasColumnType("decimal(5,2)").HasDefaultValue(0);
                entity.Property(e => e.ValorIcms).HasColumnName("VALOR_ICMS").HasColumnType("decimal(15,2)").HasDefaultValue(0);
                entity.Property(e => e.BasePis).HasColumnName("BASE_PIS").HasColumnType("decimal(15,2)");
                entity.Property(e => e.ValorPis).HasColumnName("VALOR_PIS").HasColumnType("decimal(15,2)");
                entity.Property(e => e.BaseCofins).HasColumnName("BASE_COFINS").HasColumnType("decimal(15,2)");
                entity.Property(e => e.ValorCofins).HasColumnName("VALOR_COFINS").HasColumnType("decimal(15,2)");
                
                entity.HasOne(d => d.Item)
                    .WithOne(p => p.Imposto)
                    .HasForeignKey<NFeItemImposto>(d => d.IdItem)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NFePagamento>(entity =>
            {
                entity.HasKey(e => e.IdPagamento);
                entity.ToTable("NFE_PAGAMENTO");
                entity.Property(e => e.IdPagamento).HasColumnName("ID_PAGAMENTO");
                entity.Property(e => e.IdNfe).HasColumnName("ID_NFE");
                entity.Property(e => e.TipoPagamento).HasColumnName("TIPO_PAGAMENTO").HasMaxLength(2);
                entity.Property(e => e.ValorPago).HasColumnName("VALOR_PAGO").HasColumnType("decimal(15,2)");

                entity.HasOne(d => d.NFe)
                    .WithMany(p => p.Pagamentos)
                    .HasForeignKey(d => d.IdNfe)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NFeNaturezaOperacao>(entity =>
            {
                entity.HasKey(e => e.IdNatureza);
                entity.ToTable("NFE_NATUREZA_OPERACAO");
                entity.Property(e => e.IdNatureza).HasColumnName("ID_NATUREZA");
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(255);
                entity.Property(e => e.Cfop).HasColumnName("CFOP").HasMaxLength(4);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });

            modelBuilder.Entity<TabelaIpt>(entity =>
            {
                entity.HasKey(e => e.Codigo);
                entity.ToTable("TABELA_IPT");
                entity.Property(e => e.Codigo).HasColumnName("CODIGO").HasMaxLength(20);
                entity.Property(e => e.Descricao).HasColumnName("DESCRICAO").HasMaxLength(255);
                entity.Property(e => e.Ativo).HasColumnName("ATIVO");
            });
        }
    }
}

