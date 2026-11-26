CREATE SCHEMA IF NOT EXISTS taskdev_security;

CREATE TABLE IF NOT EXISTS taskdev_security.data_protection_keys (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    encrypted_xml TEXT NOT NULL
);

REVOKE ALL ON SCHEMA taskdev_security FROM PUBLIC;
REVOKE ALL ON TABLE taskdev_security.data_protection_keys FROM PUBLIC;
