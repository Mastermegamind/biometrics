<?php
// php-api/enrollments.php

declare(strict_types=1);

header('Content-Type: application/json');

$method = $_SERVER['REQUEST_METHOD'] ?? 'GET';

$pdo = require __DIR__ . '/db.php';

function respond(int $status, array $payload): void
{
    http_response_code($status);
    echo json_encode($payload);
    exit;
}

function normalize_regno(?string $regno): string
{
    return trim((string)$regno);
}

function validate_base64(string $data): bool
{
    return $data !== '' && base64_decode($data, true) !== false;
}

function sanitize_filename(string $name): string
{
    $name = basename($name);
    return preg_replace('/[^A-Za-z0-9._-]/', '_', $name);
}

function finger_name_from_index(int $index): string
{
    return match ($index) {
        1 => 'right-thumb',
        2 => 'right-index',
        3 => 'right-middle',
        4 => 'right-ring',
        5 => 'right-little',
        6 => 'left-thumb',
        7 => 'left-index',
        8 => 'left-middle',
        9 => 'left-ring',
        10 => 'left-little',
        default => 'unknown'
    };
}

function lagos_now(): string
{
    $dt = new DateTime('now', new DateTimeZone('Africa/Lagos'));
    return $dt->format('Y-m-d H:i:s');
}

$storageDir = __DIR__ . '/../storage/fingerprints';
if (!is_dir($storageDir)) {
    @mkdir($storageDir, 0777, true);
}

if ($method === 'GET') {
    $regno = normalize_regno($_GET['regno'] ?? '');
    if ($regno === '') {
        respond(400, ['success' => false, 'message' => 'regno is required']);
    }

    $stmt = $pdo->prepare(
        'SELECT regno, finger_index, finger_name, template, template_data, image_preview, captured_at
         FROM fingerprint_enrollments
         WHERE regno = :regno
         ORDER BY finger_index'
    );
    $stmt->execute([':regno' => $regno]);
    $rows = $stmt->fetchAll();

    $records = [];
    foreach ($rows as $row) {
        $template = $row['template'] ?? '';
        $templateData = $row['template_data'] ?? '';
        $records[] = [
            'regno' => $row['regno'],
            'finger_index' => (int)$row['finger_index'],
            'finger_name' => $row['finger_name'] ?: finger_name_from_index((int)$row['finger_index']),
            'template' => $template !== '' ? base64_encode($template) : '',
            'template_data' => $templateData !== '' ? base64_encode($templateData) : '',
            'image_preview' => $row['image_preview'] ?: null,
            'captured_at' => $row['captured_at'] ?: null
        ];
    }

    respond(200, [
        'success' => true,
        'records' => $records
    ]);
}

if ($method === 'POST') {
    $raw = file_get_contents('php://input');
    $payload = json_decode($raw, true);

    if (!is_array($payload)) {
        respond(400, ['success' => false, 'message' => 'Invalid JSON']);
    }

    $regno = normalize_regno($payload['regno'] ?? '');
    $records = $payload['records'] ?? null;

    if ($regno === '' || !is_array($records) || count($records) === 0) {
        respond(400, ['success' => false, 'message' => 'Invalid payload']);
    }

    $insert = $pdo->prepare(
        'INSERT INTO fingerprint_enrollments
            (regno, finger_index, finger_name, template, template_data, image_preview, captured_at)
         VALUES
            (:regno, :finger_index, :finger_name, :template, :template_data, :image_preview, :captured_at)
         ON DUPLICATE KEY UPDATE
            finger_name = VALUES(finger_name),
            template = VALUES(template),
            template_data = VALUES(template_data),
            image_preview = VALUES(image_preview),
            captured_at = VALUES(captured_at)'
    );

    $saved = 0;
    foreach ($records as $r) {
        $fingerIndex = (int)($r['finger_index'] ?? 0);
        if ($fingerIndex < 1 || $fingerIndex > 10) {
            respond(422, ['success' => false, 'message' => 'Invalid finger value']);
        }

        $fingerName = $r['finger_name'] ?? finger_name_from_index($fingerIndex);
        $fingerName = strtolower(trim((string)$fingerName));

        $templateB64 = (string)($r['template'] ?? '');
        $templateDataB64 = (string)($r['template_data'] ?? $templateB64);

        if (!validate_base64($templateB64) && !validate_base64($templateDataB64)) {
            respond(422, ['success' => false, 'message' => 'Invalid template data']);
        }

        $templateBytes = base64_decode($templateB64, true);
        $templateDataBytes = base64_decode($templateDataB64, true);
        if ($templateBytes === false || $templateDataBytes === false) {
            respond(422, ['success' => false, 'message' => 'Invalid template data']);
        }

        $imagePreview = isset($r['image_preview']) ? sanitize_filename((string)$r['image_preview']) : null;
        $previewData = (string)($r['image_preview_data'] ?? '');

        if ($previewData !== '') {
            $decoded = base64_decode($previewData, true);
            if ($decoded !== false) {
                if ($imagePreview === null || $imagePreview === '') {
                    $imagePreview = sprintf('fingerprint_%s_%d_%s.png',
                        preg_replace('/[^A-Za-z0-9_-]/', '_', $regno),
                        $fingerIndex,
                        (new DateTime('now', new DateTimeZone('Africa/Lagos')))->format('YmdHis')
                    );
                }
                file_put_contents($storageDir . '/' . $imagePreview, $decoded);
            }
        }

        $capturedAt = isset($r['captured_at']) && $r['captured_at'] !== ''
            ? (string)$r['captured_at']
            : lagos_now();

        $insert->bindValue(':regno', $regno, PDO::PARAM_STR);
        $insert->bindValue(':finger_index', $fingerIndex, PDO::PARAM_INT);
        $insert->bindValue(':finger_name', $fingerName, PDO::PARAM_STR);
        $insert->bindValue(':template', $templateBytes, PDO::PARAM_LOB);
        $insert->bindValue(':template_data', $templateDataBytes, PDO::PARAM_LOB);
        $insert->bindValue(':image_preview', $imagePreview, $imagePreview ? PDO::PARAM_STR : PDO::PARAM_NULL);
        $insert->bindValue(':captured_at', $capturedAt, PDO::PARAM_STR);
        $insert->execute();
        $saved++;
    }

    respond(200, ['success' => true, 'message' => 'Enrollment saved', 'saved' => $saved]);
}

respond(405, ['success' => false, 'message' => 'Method not allowed']);
