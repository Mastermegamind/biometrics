<?php
// php-api/enrollment-templates.php

declare(strict_types=1);

header('Content-Type: application/json');

if (($_SERVER['REQUEST_METHOD'] ?? 'GET') !== 'GET') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Method not allowed']);
    exit;
}

$pdo = require __DIR__ . '/db.php';

$regno = trim((string)($_GET['regno'] ?? ''));
if ($regno === '') {
    http_response_code(400);
    echo json_encode(['success' => false, 'message' => 'regno is required']);
    exit;
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
        'finger_name' => $row['finger_name'] ?: null,
        'template' => $template !== '' ? base64_encode($template) : '',
        'template_data' => $templateData !== '' ? base64_encode($templateData) : '',
        'image_preview' => $row['image_preview'] ?: null,
        'captured_at' => $row['captured_at'] ?: null
    ];
}

http_response_code(200);
echo json_encode([
    'success' => true,
    'records' => $records
]);
