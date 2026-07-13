# Golden fixtures

This directory is the Swift-side source of truth for deterministic compatibility fixtures consumed by the Windows xUnit suite.

Fixtures must be generated from shared XCTest case factories, use fixed clocks and identifiers, and pass the fail-closed secret scanner before they are committed. Do not add production credentials, real user paths, real email addresses, dynamic ports, or unredacted authorization headers.

The versioned fixture set will live under `v1/` with a sorted manifest and SHA-256 checksums.
