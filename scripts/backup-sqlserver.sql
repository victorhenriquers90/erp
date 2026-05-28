-- ============================================================
-- Script de backup do banco ProjetoVarejo
-- Execute diariamente via SQL Server Agent ou Task Scheduler
-- ============================================================

DECLARE @pasta     NVARCHAR(260) = N'C:\Backups\ProjetoVarejo\';
DECLARE @arquivo   NVARCHAR(260);
DECLARE @datahora  NVARCHAR(20)  = CONVERT(NVARCHAR(20), GETDATE(), 112)
                                  + '_'
                                  + REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), ':', '');

-- Nome final: ProjetoVarejo_20250527_143022.bak
SET @arquivo = @pasta + N'ProjetoVarejo_' + @datahora + N'.bak';

-- Garante que a pasta exista (SQL Server 2012+)
EXEC xp_create_subdir @pasta;

-- Backup completo comprimido
BACKUP DATABASE [ProjetoVarejo]
    TO DISK = @arquivo
    WITH
        COMPRESSION,
        STATS = 10,
        DESCRIPTION = N'Backup automático ProjetoVarejo';

PRINT N'Backup concluído: ' + @arquivo;

-- -------------------------------------------------------
-- Limpeza: apagar backups com mais de 30 dias
-- -------------------------------------------------------
DECLARE @limite NVARCHAR(20) = CONVERT(NVARCHAR(20), DATEADD(DAY, -30, GETDATE()), 112);

DECLARE @cmd NVARCHAR(500);
SET @cmd = N'forfiles /P "' + @pasta + N'" /M "ProjetoVarejo_*.bak"'
         + N' /D -30 /C "cmd /c del @path" 2>nul';

EXEC xp_cmdshell @cmd, no_output;
