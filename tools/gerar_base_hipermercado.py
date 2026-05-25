from __future__ import annotations

import csv
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CSV_PATH = ROOT / "docs" / "dados" / "hipermercado_produtos.csv"
SQL_PATH = ROOT / "scripts" / "sql" / "seed_hipermercado_produtos.sql"

UNIDADES = {"UN": 1, "KG": 2, "G": 3, "L": 4, "ML": 5, "M": 6, "CM": 7, "CX": 8, "PCT": 9, "DZ": 10}
MARCAS = ["Casa Vale", "Popular", "Select", "Bom Preco", "Familia"]

CATEGORIAS = [
    ("Mercearia - Graos e Cereais", "Arroz, feijao, farinhas e cereais"),
    ("Mercearia - Massas e Molhos", "Massas secas, molhos e conservas"),
    ("Mercearia - Matinais", "Cafe, achocolatado, cereais e biscoitos"),
    ("Mercearia - Condimentos", "Temperos, oleos, vinagres e condimentos"),
    ("Bebidas", "Agua, refrigerantes, sucos e bebidas diversas"),
    ("Adega e Cervejas", "Cervejas, vinhos e bebidas alcoolicas"),
    ("Hortifruti", "Frutas, legumes, verduras e ovos"),
    ("Acougue e Peixaria", "Carnes bovinas, suinas, aves e pescados"),
    ("Padaria e Confeitaria", "Paes, bolos e produtos de padaria"),
    ("Frios e Laticinios", "Leites, queijos, iogurtes, frios e margarinas"),
    ("Congelados", "Pratos prontos, vegetais, pizzas e sorvetes"),
    ("Limpeza", "Limpeza domestica, lavanderia e descartaveis"),
    ("Higiene e Beleza", "Higiene pessoal, cabelo, corpo e beleza"),
    ("Bebe e Infantil", "Fraldas, alimentos infantis e cuidado infantil"),
    ("Pet Shop", "Alimentos e higiene pet"),
    ("Bazar e Utilidades", "Utensilios, organizacao e casa"),
    ("Papelaria", "Material escolar e escritorio"),
    ("Automotivo", "Produtos automotivos de conveniencia"),
]


def money(value: float | Decimal) -> Decimal:
    return Decimal(str(value)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def ean13(payload12: str) -> str:
    digits = [int(d) for d in payload12]
    total = sum(digits[::2]) + sum(d * 3 for d in digits[1::2])
    check = (10 - total % 10) % 10
    return payload12 + str(check)


def add(rows, categoria, base, preco, unidade="UN", tamanho="", marcas=None, estoque=None, minimo=None):
    marcas = marcas or [""]
    for marca in marcas:
        desc = " ".join(part for part in [base, tamanho, marca] if part).strip()
        rows.append(
            {
                "Categoria": categoria,
                "Descricao": desc[:190],
                "Unidade": unidade,
                "PrecoVenda": money(preco),
                "PrecoCusto": money(Decimal(str(preco)) * Decimal("0.68")),
                "Estoque": estoque,
                "EstoqueMinimo": minimo,
                "Ncm": "",
                "Cfop": "5102",
                "CstIcms": "102",
                "AliquotaIcms": money("0"),
            }
        )


def gerar_produtos():
    rows = []

    graos = [
        ("Arroz agulhinha tipo 1", [("1 kg", 6.99), ("2 kg", 12.99), ("5 kg", 29.90)]),
        ("Arroz parboilizado", [("1 kg", 7.49), ("5 kg", 31.90)]),
        ("Feijao carioca", [("1 kg", 8.99), ("2 kg", 16.90)]),
        ("Feijao preto", [("1 kg", 9.49), ("2 kg", 17.90)]),
        ("Lentilha", [("500 g", 9.90)]),
        ("Grao de bico", [("500 g", 11.90)]),
        ("Milho para pipoca", [("500 g", 4.99)]),
        ("Farinha de trigo", [("1 kg", 5.49), ("5 kg", 24.90)]),
        ("Farinha de mandioca", [("500 g", 6.99), ("1 kg", 12.90)]),
        ("Fuba mimoso", [("500 g", 4.79), ("1 kg", 8.49)]),
        ("Acucar cristal", [("1 kg", 4.99), ("5 kg", 22.90)]),
        ("Acucar refinado", [("1 kg", 5.49)]),
        ("Sal refinado", [("1 kg", 2.99)]),
        ("Aveia em flocos", [("500 g", 7.99)]),
    ]
    for base, tamanhos in graos:
        for tamanho, preco in tamanhos:
            add(rows, "Mercearia - Graos e Cereais", base, preco, "UN", tamanho, MARCAS)

    massas = [
        ("Macarrao espaguete", [("500 g", 4.99), ("1 kg", 8.99)]),
        ("Macarrao parafuso", [("500 g", 5.29)]),
        ("Macarrao penne", [("500 g", 6.29)]),
        ("Lasanha seca", [("500 g", 9.99)]),
        ("Molho de tomate tradicional", [("300 g", 3.49), ("500 g", 5.49)]),
        ("Extrato de tomate", [("340 g", 5.99)]),
        ("Tomate pelado", [("400 g", 8.49)]),
        ("Milho verde lata", [("170 g", 4.49)]),
        ("Ervilha lata", [("170 g", 4.29)]),
        ("Seleta de legumes", [("170 g", 5.49)]),
        ("Atum ralado", [("170 g", 8.99)]),
        ("Sardinha em oleo", [("125 g", 6.99)]),
    ]
    for base, tamanhos in massas:
        for tamanho, preco in tamanhos:
            add(rows, "Mercearia - Massas e Molhos", base, preco, "UN", tamanho, MARCAS)

    matinais = [
        ("Cafe torrado e moido", [("250 g", 10.90), ("500 g", 19.90)]),
        ("Cafe soluvel", [("100 g", 14.90)]),
        ("Achocolatado em po", [("400 g", 9.90), ("800 g", 17.90)]),
        ("Leite em po integral", [("400 g", 18.90)]),
        ("Cereal matinal chocolate", [("250 g", 12.90)]),
        ("Granola tradicional", [("500 g", 16.90)]),
        ("Biscoito cream cracker", [("350 g", 5.99)]),
        ("Biscoito recheado chocolate", [("130 g", 2.99)]),
        ("Biscoito maisena", [("350 g", 6.49)]),
        ("Torrada integral", [("160 g", 6.99)]),
        ("Geleia de morango", [("230 g", 12.90)]),
        ("Mel", [("500 g", 24.90)]),
    ]
    for base, tamanhos in matinais:
        for tamanho, preco in tamanhos:
            add(rows, "Mercearia - Matinais", base, preco, "UN", tamanho, MARCAS)

    condimentos = [
        ("Oleo de soja", "900 ml", 7.49),
        ("Oleo de milho", "900 ml", 10.90),
        ("Azeite extra virgem", "500 ml", 31.90),
        ("Vinagre alcool", "750 ml", 3.99),
        ("Vinagre maca", "750 ml", 6.99),
        ("Maionese", "500 g", 9.90),
        ("Ketchup", "400 g", 8.49),
        ("Mostarda", "200 g", 6.49),
        ("Molho shoyu", "150 ml", 6.99),
        ("Tempero completo", "300 g", 5.99),
        ("Pimenta do reino", "50 g", 7.49),
        ("Alho triturado", "200 g", 8.90),
    ]
    for base, tamanho, preco in condimentos:
        add(rows, "Mercearia - Condimentos", base, preco, "UN", tamanho, MARCAS)

    bebidas = [
        ("Agua mineral sem gas", [("500 ml", 1.99), ("1,5 l", 3.49), ("5 l", 9.90)]),
        ("Agua mineral com gas", [("500 ml", 2.49)]),
        ("Refrigerante cola", [("350 ml", 3.99), ("2 l", 9.49)]),
        ("Refrigerante guarana", [("350 ml", 3.79), ("2 l", 8.99)]),
        ("Refrigerante laranja", [("2 l", 8.49)]),
        ("Suco nectar uva", [("1 l", 7.99)]),
        ("Suco nectar laranja", [("1 l", 7.99)]),
        ("Cha pronto pessego", [("1,5 l", 8.99)]),
        ("Energetico", [("269 ml", 7.99), ("473 ml", 11.90)]),
        ("Agua de coco", [("1 l", 12.90)]),
        ("Isotonico limao", [("500 ml", 5.99)]),
        ("Leite UHT integral", [("1 l", 5.49)]),
        ("Leite UHT desnatado", [("1 l", 5.69)]),
    ]
    for base, tamanhos in bebidas:
        for tamanho, preco in tamanhos:
            add(rows, "Bebidas", base, preco, "UN", tamanho, MARCAS)

    adega = [
        ("Cerveja pilsen lata", [("350 ml", 3.49), ("473 ml", 4.99)]),
        ("Cerveja puro malte lata", [("350 ml", 4.49)]),
        ("Cerveja long neck", [("330 ml", 5.99)]),
        ("Vinho tinto seco", [("750 ml", 34.90)]),
        ("Vinho tinto suave", [("750 ml", 29.90)]),
        ("Vinho branco seco", [("750 ml", 32.90)]),
        ("Espumante brut", [("750 ml", 49.90)]),
        ("Vodka", [("1 l", 39.90)]),
        ("Cachaca prata", [("965 ml", 18.90)]),
        ("Whisky nacional", [("1 l", 69.90)]),
    ]
    for base, tamanhos in adega:
        for tamanho, preco in tamanhos:
            add(rows, "Adega e Cervejas", base, preco, "UN", tamanho, MARCAS[:4])

    hortifruti = [
        ("Banana prata", 6.99), ("Banana nanica", 5.99), ("Maca nacional", 9.99),
        ("Maca gala", 11.90), ("Laranja pera", 4.99), ("Limao taiti", 6.49),
        ("Mamao formosa", 7.99), ("Manga palmer", 8.99), ("Uva sem semente", 17.90),
        ("Melancia", 3.49), ("Melao amarelo", 6.99), ("Abacaxi", 8.99),
        ("Tomate italiano", 8.99), ("Cebola", 5.49), ("Batata inglesa", 5.99),
        ("Batata doce", 5.49), ("Cenoura", 4.99), ("Beterraba", 4.99),
        ("Pepino", 5.99), ("Abobrinha", 6.49), ("Alface crespa", 3.99),
        ("Alface americana", 5.99), ("Brocolis", 8.99), ("Couve manteiga", 3.49),
        ("Ovos brancos duzia", 10.90), ("Ovos vermelhos duzia", 12.90),
    ]
    for base, preco in hortifruti:
        unidade = "DZ" if "duzia" in base else "KG"
        add(rows, "Hortifruti", base, preco, unidade, "", [""])

    carnes = [
        ("Acem bovino", 32.90), ("Patinho bovino", 42.90), ("Coxao mole", 44.90),
        ("Contra file", 59.90), ("Carne moida bovina", 35.90), ("Costela bovina", 29.90),
        ("File de frango", 21.90), ("Coxa e sobrecoxa de frango", 13.90),
        ("Frango inteiro resfriado", 10.90), ("Linguica toscana", 22.90),
        ("Pernil suino", 24.90), ("Bisteca suina", 23.90), ("Costelinha suina", 28.90),
        ("File de tilapia", 49.90), ("Sardinha fresca", 18.90), ("Camarao limpo", 69.90),
    ]
    for base, preco in carnes:
        add(rows, "Acougue e Peixaria", base, preco, "KG", "", [""])

    padaria = [
        ("Pao frances", 17.90, "KG"), ("Pao de forma tradicional", 7.99, "UN"),
        ("Pao integral", 9.99, "UN"), ("Pao de queijo congelado", 18.90, "UN"),
        ("Bolo de cenoura", 24.90, "UN"), ("Bolo de chocolate", 27.90, "UN"),
        ("Rosca doce", 12.90, "UN"), ("Sonho recheado", 4.99, "UN"),
        ("Croissant", 6.99, "UN"), ("Torta salgada pedaco", 8.99, "UN"),
    ]
    for base, preco, unidade in padaria:
        add(rows, "Padaria e Confeitaria", base, preco, unidade, "", [""])

    laticinios = [
        ("Queijo mussarela fatiado", "200 g", 12.90), ("Queijo prato fatiado", "200 g", 13.90),
        ("Presunto cozido fatiado", "200 g", 9.99), ("Mortadela fatiada", "200 g", 6.99),
        ("Iogurte morango", "900 g", 12.90), ("Iogurte natural", "170 g", 3.49),
        ("Requeijao cremoso", "200 g", 8.99), ("Manteiga", "200 g", 11.90),
        ("Margarina", "500 g", 7.99), ("Creme de leite", "200 g", 4.49),
        ("Leite condensado", "395 g", 6.99), ("Queijo parmesao ralado", "50 g", 5.99),
    ]
    for base, tamanho, preco in laticinios:
        add(rows, "Frios e Laticinios", base, preco, "UN", tamanho, MARCAS)

    congelados = [
        ("Pizza mussarela congelada", "460 g", 18.90), ("Pizza calabresa congelada", "460 g", 19.90),
        ("Hamburguer bovino", "672 g", 24.90), ("Batata palito congelada", "1,05 kg", 21.90),
        ("Lasanha bolonhesa congelada", "600 g", 16.90), ("Nuggets de frango", "700 g", 24.90),
        ("Vegetais seleta congelada", "300 g", 7.99), ("Sorvete creme", "1,5 l", 24.90),
        ("Sorvete chocolate", "1,5 l", 24.90), ("Polpa de fruta", "400 g", 9.99),
    ]
    for base, tamanho, preco in congelados:
        add(rows, "Congelados", base, preco, "UN", tamanho, MARCAS)

    limpeza = [
        ("Detergente neutro", "500 ml", 2.49), ("Detergente limao", "500 ml", 2.49),
        ("Sabao em po", "1,6 kg", 18.90), ("Sabao liquido roupas", "3 l", 32.90),
        ("Amaciante", "2 l", 12.90), ("Agua sanitaria", "2 l", 6.49),
        ("Desinfetante lavanda", "2 l", 8.99), ("Limpador multiuso", "500 ml", 6.99),
        ("Esponja dupla face", "4 un", 5.99), ("Pano multiuso", "5 un", 7.99),
        ("Saco lixo 30 l", "30 un", 9.99), ("Saco lixo 100 l", "15 un", 14.90),
        ("Papel toalha", "2 rolos", 8.99), ("Alcool 70", "1 l", 9.99),
    ]
    for base, tamanho, preco in limpeza:
        add(rows, "Limpeza", base, preco, "UN", tamanho, MARCAS)

    higiene = [
        ("Sabonete", "85 g", 2.99), ("Shampoo cabelo normal", "350 ml", 13.90),
        ("Condicionador cabelo normal", "350 ml", 14.90), ("Creme dental", "90 g", 4.99),
        ("Escova dental media", "1 un", 6.99), ("Fio dental", "50 m", 7.99),
        ("Desodorante aerosol", "150 ml", 12.90), ("Papel higienico folha dupla", "12 rolos", 18.90),
        ("Absorvente com abas", "16 un", 8.99), ("Aparelho barbear descartavel", "2 un", 9.99),
        ("Hidratante corporal", "400 ml", 19.90), ("Protetor solar FPS 30", "120 ml", 34.90),
    ]
    for base, tamanho, preco in higiene:
        add(rows, "Higiene e Beleza", base, preco, "UN", tamanho, MARCAS)

    bebe = [
        ("Fralda infantil P", "30 un", 32.90), ("Fralda infantil M", "28 un", 34.90),
        ("Fralda infantil G", "26 un", 36.90), ("Lenco umedecido", "48 un", 8.99),
        ("Shampoo infantil", "200 ml", 12.90), ("Sabonete infantil", "80 g", 3.99),
        ("Papinha legumes", "115 g", 7.99), ("Cereal infantil", "230 g", 14.90),
    ]
    for base, tamanho, preco in bebe:
        add(rows, "Bebe e Infantil", base, preco, "UN", tamanho, MARCAS[:3])

    pet = [
        ("Racao cao adulto carne", "10 kg", 89.90), ("Racao cao adulto frango", "10 kg", 84.90),
        ("Racao cao filhote", "3 kg", 39.90), ("Racao gato adulto peixe", "3 kg", 42.90),
        ("Racao gato castrado", "3 kg", 49.90), ("Areia sanitaria gatos", "4 kg", 16.90),
        ("Bifinho cao", "500 g", 18.90), ("Sachê gato", "85 g", 3.49),
        ("Shampoo pet", "500 ml", 19.90), ("Tapete higienico", "30 un", 59.90),
    ]
    for base, tamanho, preco in pet:
        add(rows, "Pet Shop", base, preco, "UN", tamanho, MARCAS[:4])

    bazar = [
        ("Copo descartavel", "100 un", 8.99), ("Prato descartavel", "10 un", 5.99),
        ("Talher descartavel", "50 un", 7.99), ("Pote plastico", "1 l", 9.99),
        ("Garrafa termica", "1 l", 39.90), ("Pilha alcalina AA", "4 un", 18.90),
        ("Lampada LED", "9 W", 8.99), ("Extensao eletrica", "3 m", 29.90),
        ("Vela aniversario", "24 un", 6.99), ("Carvao vegetal", "4 kg", 17.90),
        ("Papel aluminio", "30 cm x 4 m", 7.99), ("Filme PVC", "28 cm x 15 m", 8.99),
    ]
    for base, tamanho, preco in bazar:
        add(rows, "Bazar e Utilidades", base, preco, "UN", tamanho, MARCAS[:3])

    papelaria = [
        ("Caderno universitario", "96 folhas", 14.90), ("Caneta esferografica azul", "1 un", 2.49),
        ("Lapis preto", "1 un", 1.49), ("Borracha branca", "1 un", 1.99),
        ("Cola branca", "90 g", 4.99), ("Papel sulfite A4", "500 folhas", 29.90),
        ("Envelope oficio", "10 un", 6.99), ("Fita adesiva", "45 m", 5.99),
    ]
    for base, tamanho, preco in papelaria:
        add(rows, "Papelaria", base, preco, "UN", tamanho, MARCAS[:3])

    automotivo = [
        ("Oleo motor", "1 l", 38.90), ("Aditivo radiador", "1 l", 16.90),
        ("Limpador para-brisa", "100 ml", 7.99), ("Cera automotiva", "200 g", 19.90),
        ("Pano microfibra", "2 un", 12.90), ("Odorizador automotivo", "1 un", 8.99),
    ]
    for base, tamanho, preco in automotivo:
        add(rows, "Automotivo", base, preco, "UN", tamanho, MARCAS[:3])

    for idx, row in enumerate(rows, start=1):
        row["Codigo"] = f"HP{idx:05d}"
        row["CodigoBarras"] = ean13(f"20{idx:010d}")
        row["Estoque"] = row["Estoque"] if row["Estoque"] is not None else Decimal(25 + (idx % 90))
        row["EstoqueMinimo"] = row["EstoqueMinimo"] if row["EstoqueMinimo"] is not None else Decimal(5 + (idx % 8))

    return rows


def sql_string(value: str | None) -> str:
    if value is None or value == "":
        return "NULL"
    return "N'" + str(value).replace("'", "''") + "'"


def gerar_csv(rows):
    CSV_PATH.parent.mkdir(parents=True, exist_ok=True)
    campos = [
        "Categoria",
        "Codigo",
        "CodigoBarras",
        "Descricao",
        "Unidade",
        "PrecoCusto",
        "PrecoVenda",
        "Estoque",
        "EstoqueMinimo",
        "Ncm",
        "Cfop",
        "CstIcms",
        "AliquotaIcms",
    ]
    with CSV_PATH.open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=campos, delimiter=";")
        writer.writeheader()
        writer.writerows(rows)


def gerar_sql(rows):
    SQL_PATH.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "SET XACT_ABORT ON;",
        "BEGIN TRAN;",
        "",
        "MERGE dbo.Categorias AS alvo",
        "USING (VALUES",
    ]
    cats = ",\n".join(f"    ({sql_string(nome)}, {sql_string(desc)})" for nome, desc in CATEGORIAS)
    lines.append(cats)
    lines.extend(
        [
            ") AS src(Nome, Descricao)",
            "ON alvo.Nome = src.Nome",
            "WHEN NOT MATCHED THEN",
            "    INSERT (CriadoEm, AtualizadoEm, Ativo, Nome, Descricao)",
            "    VALUES (SYSDATETIME(), NULL, 1, src.Nome, src.Descricao);",
            "",
            "CREATE TABLE #ProdutosSeed (",
            "    Categoria NVARCHAR(80) NOT NULL,",
            "    Codigo NVARCHAR(30) NOT NULL,",
            "    CodigoBarras NVARCHAR(50) NULL,",
            "    Descricao NVARCHAR(200) NOT NULL,",
            "    Unidade INT NOT NULL,",
            "    PrecoCusto DECIMAL(18,4) NOT NULL,",
            "    PrecoVenda DECIMAL(18,4) NOT NULL,",
            "    Estoque DECIMAL(18,4) NOT NULL,",
            "    EstoqueMinimo DECIMAL(18,4) NOT NULL,",
            "    Ncm NVARCHAR(10) NULL,",
            "    Cfop NVARCHAR(4) NOT NULL,",
            "    CstIcms NVARCHAR(3) NOT NULL,",
            "    AliquotaIcms DECIMAL(18,4) NOT NULL",
            ");",
            "",
        ]
    )

    chunk = 450
    for start in range(0, len(rows), chunk):
        batch = rows[start : start + chunk]
        values = []
        for r in batch:
            values.append(
                "    ("
                + ", ".join(
                    [
                        sql_string(r["Categoria"]),
                        sql_string(r["Codigo"]),
                        sql_string(r["CodigoBarras"]),
                        sql_string(r["Descricao"]),
                        str(UNIDADES[r["Unidade"]]),
                        str(r["PrecoCusto"]),
                        str(r["PrecoVenda"]),
                        str(r["Estoque"]),
                        str(r["EstoqueMinimo"]),
                        sql_string(r["Ncm"]),
                        sql_string(r["Cfop"]),
                        sql_string(r["CstIcms"]),
                        str(r["AliquotaIcms"]),
                    ]
                )
                + ")"
            )
        lines.append("INSERT INTO #ProdutosSeed (Categoria, Codigo, CodigoBarras, Descricao, Unidade, PrecoCusto, PrecoVenda, Estoque, EstoqueMinimo, Ncm, Cfop, CstIcms, AliquotaIcms)")
        lines.append("VALUES")
        lines.append(",\n".join(values) + ";")
        lines.append("")

    lines.extend(
        [
            "MERGE dbo.Produtos AS alvo",
            "USING (",
            "    SELECT p.*, c.Id AS CategoriaId",
            "    FROM #ProdutosSeed p",
            "    LEFT JOIN dbo.Categorias c ON c.Nome = p.Categoria",
            ") AS src",
            "ON alvo.Codigo = src.Codigo",
            "WHEN NOT MATCHED THEN",
            "    INSERT (CriadoEm, AtualizadoEm, Ativo, Codigo, CodigoBarras, Descricao, CategoriaId, Unidade, PrecoCusto, PrecoVenda, Estoque, EstoqueMinimo, ControlaEstoque, PermiteVendaFracionada, Ncm, Cest, Cfop, Origem, CstIcms, AliquotaIcms, CstPisCofins)",
            "    VALUES (SYSDATETIME(), NULL, 1, src.Codigo, src.CodigoBarras, src.Descricao, src.CategoriaId, src.Unidade, src.PrecoCusto, src.PrecoVenda, src.Estoque, src.EstoqueMinimo, 1, CASE WHEN src.Unidade = 2 THEN 1 ELSE 0 END, src.Ncm, NULL, src.Cfop, N'0', src.CstIcms, src.AliquotaIcms, N'49')",
            "WHEN MATCHED AND alvo.CodigoBarras IS NULL THEN",
            "    UPDATE SET alvo.CodigoBarras = src.CodigoBarras, alvo.AtualizadoEm = SYSDATETIME();",
            "",
            "DROP TABLE #ProdutosSeed;",
            "COMMIT;",
            "",
            "SELECT COUNT(*) AS ProdutosHipermercadoSeed FROM dbo.Produtos WHERE Codigo LIKE 'HP%';",
        ]
    )

    SQL_PATH.write_text("\n".join(lines), encoding="utf-8-sig")


def main():
    rows = gerar_produtos()
    gerar_csv(rows)
    gerar_sql(rows)
    print(f"Produtos gerados: {len(rows)}")
    print(CSV_PATH)
    print(SQL_PATH)


if __name__ == "__main__":
    main()
