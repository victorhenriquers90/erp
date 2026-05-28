namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// DataGridView pré-estilizado: zebra suave, hover, header flat, sem 3D.
/// </summary>
public class StyledGrid : DataGridView
{
    public StyledGrid()
    {
        // Layout
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        BackgroundColor = Tema.CorCard;
        GridColor = Tema.CorBordaSuave;

        // Comportamento
        ReadOnly = true;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        RowHeadersVisible = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        MultiSelect = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        ShowCellToolTips = true;
        EnableHeadersVisualStyles = false;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ScrollBars = ScrollBars.Both;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        AdvancedCellBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
        AdvancedCellBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
        AdvancedCellBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;

        // Header
        ColumnHeadersDefaultCellStyle.BackColor = Tema.CorCardAlt;
        ColumnHeadersDefaultCellStyle.ForeColor = Tema.CorTextoMedio;
        ColumnHeadersDefaultCellStyle.Font = new Font(Tema.FontFamily, 9, FontStyle.Bold);
        ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 7, 10, 7);
        ColumnHeadersDefaultCellStyle.SelectionBackColor = Tema.CorCardAlt;
        ColumnHeadersDefaultCellStyle.SelectionForeColor = Tema.CorTextoMedio;
        ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        ColumnHeadersHeight = 40;

        // Linhas
        DefaultCellStyle.BackColor = Tema.CorCard;
        DefaultCellStyle.ForeColor = Tema.CorTextoEscuro;
        DefaultCellStyle.Font = Tema.FontCorpo;
        DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
        DefaultCellStyle.SelectionBackColor = Tema.CorPrimariaSoft;
        DefaultCellStyle.SelectionForeColor = Tema.CorTextoEscuro;
        DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        AlternatingRowsDefaultCellStyle.BackColor = Tema.CorCardAlt;
        AlternatingRowsDefaultCellStyle.SelectionBackColor = Tema.CorPrimariaSoft;
        AlternatingRowsDefaultCellStyle.SelectionForeColor = Tema.CorTextoEscuro;

        RowTemplate.Height = 36;
        RowsDefaultCellStyle.Padding = new Padding(10, 5, 10, 5);

        // Hover (CellMouseEnter/Leave)
        int linhaAnterior = -1;
        CellMouseEnter += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            if (linhaAnterior >= 0 && linhaAnterior < Rows.Count && !Rows[linhaAnterior].Selected)
                Rows[linhaAnterior].DefaultCellStyle.BackColor = e.RowIndex % 2 == 0 ? Tema.CorCard : Tema.CorCardAlt;
            if (!Rows[e.RowIndex].Selected)
                Rows[e.RowIndex].DefaultCellStyle.BackColor = Tema.CorFundo;
            linhaAnterior = e.RowIndex;
        };
        CellMouseLeave += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            if (!Rows[e.RowIndex].Selected)
                Rows[e.RowIndex].DefaultCellStyle.BackColor = e.RowIndex % 2 == 0 ? Tema.CorCard : Tema.CorCardAlt;
        };
    }
}
