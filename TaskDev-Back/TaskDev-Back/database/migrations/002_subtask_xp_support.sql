-- Migration 002: Suporte a pontuação fracionada de subtarefas em xp_events

-- 1. Remove a unicidade de task_id em xp_events para permitir múltiplos eventos (frações) por tarefa
ALTER TABLE xp_events DROP CONSTRAINT IF EXISTS xp_events_task_id_key;

-- 2. Adiciona coluna subtask_id para identificar pontos originados de minitasks/subtarefas
ALTER TABLE xp_events ADD COLUMN IF NOT EXISTS subtask_id UUID NULL;

-- 3. Adiciona foreign key e constraint de unicidade para subtask_id
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'xp_events_subtask_fk') THEN
        ALTER TABLE xp_events
        ADD CONSTRAINT xp_events_subtask_fk FOREIGN KEY (subtask_id) REFERENCES subtasks(id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'xp_events_subtask_unique') THEN
        ALTER TABLE xp_events
        ADD CONSTRAINT xp_events_subtask_unique UNIQUE (subtask_id);
    END IF;
END $$;

-- 4. Cria índices auxiliares para performance
CREATE INDEX IF NOT EXISTS idx_xp_events_task_id ON xp_events(task_id);
CREATE INDEX IF NOT EXISTS idx_xp_events_subtask_id ON xp_events(subtask_id);
