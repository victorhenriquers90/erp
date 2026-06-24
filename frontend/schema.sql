-- Regente — Schema Supabase
-- Execute este arquivo no SQL Editor do seu projeto Supabase

-- ─── Tabelas ─────────────────────────────────────────────────────────────────

create table if not exists turmas (
  id          uuid primary key default gen_random_uuid(),
  user_id     uuid references auth.users(id) on delete cascade not null,
  nome        text not null,
  disciplina  text not null default '',
  ano         text not null default '',
  created_at  timestamptz not null default now()
);

create table if not exists alunos (
  id          uuid primary key default gen_random_uuid(),
  user_id     uuid references auth.users(id) on delete cascade not null,
  turma_id    uuid references turmas(id) on delete cascade not null,
  nome        text not null,
  matricula   text not null default '',
  created_at  timestamptz not null default now()
);

create table if not exists avaliacoes (
  id          uuid primary key default gen_random_uuid(),
  user_id     uuid references auth.users(id) on delete cascade not null,
  turma_id    uuid references turmas(id) on delete cascade not null,
  titulo      text not null,
  peso        numeric not null default 1,
  data        date not null default current_date
);

create table if not exists notas (
  id            uuid primary key default gen_random_uuid(),
  user_id       uuid references auth.users(id) on delete cascade not null,
  avaliacao_id  uuid references avaliacoes(id) on delete cascade not null,
  aluno_id      uuid references alunos(id) on delete cascade not null,
  valor         numeric not null,
  unique (avaliacao_id, aluno_id)
);

create table if not exists frequencias (
  id        uuid primary key default gen_random_uuid(),
  user_id   uuid references auth.users(id) on delete cascade not null,
  turma_id  uuid references turmas(id) on delete cascade not null,
  aluno_id  uuid references alunos(id) on delete cascade not null,
  data      date not null,
  presente  boolean not null,
  unique (aluno_id, data)
);

create table if not exists tutorias (
  id        uuid primary key default gen_random_uuid(),
  user_id   uuid references auth.users(id) on delete cascade not null,
  aluno_id  uuid references alunos(id) on delete cascade not null,
  data      date not null default current_date,
  titulo    text not null default '',
  notas     text not null default '',
  acao      text not null default '',
  status    text not null default 'aberto' check (status in ('aberto','em_andamento','concluido'))
);

-- ─── Row Level Security ───────────────────────────────────────────────────────

alter table turmas     enable row level security;
alter table alunos     enable row level security;
alter table avaliacoes enable row level security;
alter table notas      enable row level security;
alter table frequencias enable row level security;
alter table tutorias   enable row level security;

-- Cada professor acessa apenas seus próprios dados
drop policy if exists "professor_turmas"      on turmas;
drop policy if exists "professor_alunos"      on alunos;
drop policy if exists "professor_avaliacoes"  on avaliacoes;
drop policy if exists "professor_notas"       on notas;
drop policy if exists "professor_frequencias" on frequencias;
drop policy if exists "professor_tutorias"    on tutorias;

create policy "professor_turmas"      on turmas      for all using (auth.uid() = user_id);
create policy "professor_alunos"      on alunos      for all using (auth.uid() = user_id);
create policy "professor_avaliacoes"  on avaliacoes  for all using (auth.uid() = user_id);
create policy "professor_notas"       on notas       for all using (auth.uid() = user_id);
create policy "professor_frequencias" on frequencias for all using (auth.uid() = user_id);
create policy "professor_tutorias"    on tutorias    for all using (auth.uid() = user_id);
