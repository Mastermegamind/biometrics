<?php
// php-api/attendance-clockout.php

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

// Find open attendance record
$existingStmt = $pdo->prepare('SELECT id, time_in FROM attendance_records WHERE regno = :regno AND date = :date AND time_out IS NULL ORDER BY time_in DESC LIMIT 1');
$existingStmt->execute([':regno' => $regno, ':date' => $date]);
$existing = $existingStmt->fetch();

if (!$existing) {
    respond(200, [
        'success' => true,
        'message' => 'Not clocked in',
        'student' => $student ? [
            'regNo' => $student['regno'],
            'name' => $student['name'],
            'className' => $student['class_name'],
            'passportUrl' => $student['passport_url'] ?? null
        ] : null,
        'clockInTime' => null,
        'clockOutTime' => null,
        'duration' => null,
        'notClockedIn' => true
    ]);
}

$update = $pdo->prepare('UPDATE attendance_records SET time_out = :time_out, device_id = :device_id WHERE id = :id');
$update->execute([
    ':time_out' => $timestamp,
    ':device_id' => $deviceId,
    ':id' => $existing['id']
]);

$timeIn = new DateTime($existing['time_in']);
$timeOut = new DateTime($timestamp);
$duration = $timeOut->getTimestamp() - $timeIn->getTimestamp();

respond(200, [
    'success' => true,
    'message' => 'Clock-out successful',
    'student' => $student ? [
        'regNo' => $student['regno'],
        'name' => $student['name'],
        'className' => $student['class_name'],
        'passportUrl' => $student['passport_url'] ?? null
    ] : null,
    'clockInTime' => $existing['time_in'],
    'clockOutTime' => $timestamp,
    'duration' => $duration,
    'notClockedIn' => false
]);
