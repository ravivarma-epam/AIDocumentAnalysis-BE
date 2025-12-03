--liquibase formatted sql

--changeset testuser:001_create_demo_table
CREATE TABLE demo_table (
    id SERIAL PRIMARY KEY,
    message TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

--changeset testuser:002_insert_initial_data
INSERT INTO demo_table (message) VALUES 
('Hello from Liquibase!'),
('This migration was applied automatically.'),
('Your changelogs are working!');
