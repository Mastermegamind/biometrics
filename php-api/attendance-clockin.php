<?php
// php-api/attendance-clockin.php

declare(strict_types=1);

header('Content-Type: application/json');

if (($_SERVER['REQUEST_METHOD'] ?? 'POST') !== 'POST') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Method not allowed']);
    exit;
}

$pdo = require __DIR__ . '/db.php';

function respond(int $status, array $payload): void
{
    http_response_code($status);
    echo json_encode($payload);
    exit;
}

function validate_base64(string $data): bool
{
    return $data !== '' && base64_decode($data, true) !== false;
}

function lagos_now(): string
{
    $dt = new DateTime('now', new DateTimeZone('Africa/Lagos'));
    return $dt->format('Y-m-d H:i:s');
}

function lagos_date(string $timestamp): string
{
    $dt = new DateTime($timestamp, new DateTimeZone('Africa/Lagos'));
    return $dt->format('Y-m-d');
}

function match_fingerprint(string $sampleBase64): ?array
{
    $matcherUrl = getenv('BIO_MATCHER_URL') ?: 'http://localhost:5085/match';

    $ch = curl_init($matcherUrl);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
    curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode([
        'templateBase64' => $sampleBase64
    ]));

    $resp = curl_exec($ch);
    $status = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    if ($resp === false || $status < 200 || $status >= 300) {
        return null;
    }

    $data = json_decode($resp, true);
    if (!is_array($data) || empty($data['success'])) {
        return null;
    }

    return [
        'regno' => $data['regno'] ?? null,
        'finger_index' => $data['finger_index'] ?? null,
        'match_score' => $data['match_score'] ?? null,
        'far' => $data['far'] ?? null
    ];
}

$raw = file_get_contents('php://input');
$payload = json_decode($raw, true);

if (!is_array($payload)) {
    respond(400, ['success' => false, 'message' => 'Invalid JSON']);
}

$templateBase64 = (string)($payload['templateBase64'] ?? '');
$timestamp = (string)($payload['timestamp'] ?? '');
$deviceId = (string)($payload['deviceId'] ?? '');

if (!validate_base64($templateBase64)) {
    respond(422, ['success' => false, 'message' => 'Invalid template']);
}

if ($timestamp === '') {
    $timestamp = lagos_now();
}

$match = match_fingerprint($templateBase64);
if ($match === null) {
    respond(200, [
        'success' => false,
        'message' => 'No match'
    ]);
}

$regno = $match['regno'];
$date = lagos_date($timestamp);

// Load student info
$studentStmt = $pdo->prepare('SELECT regno, name, class_name, passport_url FROM students WHERE regno = :regno LIMIT 1');
$studentStmt->execute([':regno' => $regno]);
$student = $studentStmt->fetch();

// Check if already clocked in today
$existingStmt = $pdo->prepare('SELECT id, time_in FROM attendance_records WHERE regno = :regno AND date = :date LIMIT 1');
$existingStmt->execute([':regno' => $regno, ':date' => $date]);
$existing = $existingStmt->fetch();

if ($existing) {
    respond(200, [
        'success' => true,
        'message' => 'Already clocked in',
        'student' => $student ? [
            'regNo' => $student['regno'],
            'name' => $student['name'],
            'className' => $student['class_name'],
            'passportUrl' => $student['passport_url'] ?? null
        ] : null,
        'clockInTime' => $existing['time_in'],
        'alreadyClockedIn' => true
    ]);
}

$insert = $pdo->prepare(
    'INSERT INTO attendance_records
        (regno, name, class_name, date, time_in, device_id, match_score, match_far, finger_index, created_at)
     VALUES
        (:regno, :name, :class_name, :date, :time_in, :device_id, :match_score, :match_far, :finger_index, :created_at)'
);

$insert->execute([
    ':regno' => $regno,
    ':name' => $student['name'] ?? '',
    ':class_name' => $student['class_name'] ?? '',
    ':date' => $date,
    ':time_in' => $timestamp,
    ':device_id' => $deviceId,
    ':match_score' => $match['match_score'] ?? null,
    ':match_far' => $match['far'] ?? null,
    ':finger_index' => $match['finger_index'] ?? null,
    ':created_at' => lagos_now()
]);

respond(200, [
    'success' => true,
    'message' => 'Clock-in successful',
    'student' => $student ? [
        'regNo' => $student['regno'],
        'name' => $student['name'],
        'className' => $student['class_name'],
        'passportUrl' => $student['passport_url'] ?? null
    ] : null,
    'clockInTime' => $timestamp,
    'alreadyClockedIn' => false
]);
