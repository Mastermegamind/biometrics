<?php
// php-api/db.php
// Centralized PDO creation.

declare(strict_types=1);

$DB_HOST = getenv('BIO_DB_HOST') ?: '127.0.0.1';
$DB_PORT = getenv('BIO_DB_PORT') ?: '3319';
$DB_NAME = getenv('BIO_DB_NAME') ?: 'mda_biometrics';
$DB_USER = getenv('BIO_DB_USER') ?: 'root';
$DB_PASS = getenv('BIO_DB_PASS') ?: '';

$dsn = "mysql:host={$DB_HOST};port={$DB_PORT};dbname={$DB_NAME};charset=utf8mb4";

$options = [
    PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
    PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    PDO::ATTR_EMULATE_PREPARES => false
];

return new PDO($dsn, $DB_USER, $DB_PASS, $options);
