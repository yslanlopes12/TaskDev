-- Tabela users
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY NOT NULL DEFAULT gen_random_uuid(),
    first_name VARCHAR(80) NOT NULL,
    last_name VARCHAR(80) NOT NULL,
    username VARCHAR(60) NOT NULL,
    password_hash TEXT NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'MEMBER',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    auth_version INTEGER NOT NULL DEFAULT 1,
    timezone VARCHAR(100) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT users_username_unique UNIQUE (username),
    CONSTRAINT users_role_check CHECK (role IN ('ADMIN', 'OPERATOR', 'MEMBER'))
);

-- Tabela spaces
CREATE TABLE IF NOT EXISTS spaces (
    id UUID PRIMARY KEY NOT NULL DEFAULT gen_random_uuid(),
    owner_user_id UUID NOT NULL,
    name VARCHAR(120) NOT NULL,
    icon VARCHAR(50) NULL,
    color VARCHAR(20) NULL,
    visibility VARCHAR(20) NOT NULL DEFAULT 'PRIVATE',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT spaces_owner_fk FOREIGN KEY (owner_user_id) REFERENCES users(id),
    CONSTRAINT spaces_visibility_check CHECK (visibility IN ('PRIVATE', 'SHARED'))
);

-- Tabela space_members
CREATE TABLE IF NOT EXISTS space_members (
    space_id UUID NOT NULL,
    user_id UUID NOT NULL,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (space_id, user_id),
    CONSTRAINT space_members_space_fk FOREIGN KEY (space_id) REFERENCES spaces(id) ON DELETE CASCADE,
    CONSTRAINT space_members_user_fk FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Tabela tasks
CREATE TABLE IF NOT EXISTS tasks (
    id UUID PRIMARY KEY NOT NULL DEFAULT gen_random_uuid(),
    space_id UUID NOT NULL,
    created_by_user_id UUID NOT NULL,
    assignee_user_id UUID NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    priority VARCHAR(20) NOT NULL DEFAULT 'MEDIUM',
    due_date DATE NULL,
    position BIGINT NOT NULL DEFAULT 0,
    xp_awarded BOOLEAN NOT NULL DEFAULT FALSE,
    completed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT tasks_space_fk FOREIGN KEY (space_id) REFERENCES spaces(id) ON DELETE CASCADE,
    CONSTRAINT tasks_creator_fk FOREIGN KEY (created_by_user_id) REFERENCES users(id),
    CONSTRAINT tasks_assignee_fk FOREIGN KEY (assignee_user_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT tasks_status_check CHECK (status IN ('PENDING', 'IN_PROGRESS', 'DONE')),
    CONSTRAINT tasks_priority_check CHECK (priority IN ('LOW', 'MEDIUM', 'HIGH', 'URGENT'))
);

CREATE INDEX IF NOT EXISTS idx_tasks_space_status_position ON tasks(space_id, status, position);

-- Tabela subtasks
CREATE TABLE IF NOT EXISTS subtasks (
    id UUID PRIMARY KEY NOT NULL DEFAULT gen_random_uuid(),
    task_id UUID NOT NULL,
    title VARCHAR(200) NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    position BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT subtasks_task_fk FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE
);

-- Tabela agent_configs
CREATE TABLE IF NOT EXISTS agent_configs (
    user_id UUID PRIMARY KEY NOT NULL,
    name VARCHAR(100) NOT NULL,
    endpoint_url TEXT NOT NULL,
    provider_type VARCHAR(30) NOT NULL,
    model VARCHAR(120) NULL,
    encrypted_api_key TEXT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT agent_configs_user_fk FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Tabela xp_events
CREATE TABLE IF NOT EXISTS xp_events (
    id UUID PRIMARY KEY NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    task_id UUID NULL UNIQUE,
    points INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT xp_events_user_fk FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT xp_events_task_fk FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE SET NULL,
    CONSTRAINT xp_events_points_positive CHECK (points > 0)
);
