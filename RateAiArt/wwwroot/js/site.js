/**
 * RateAiArt — Main Application Script
 * Handles: image upload, API calls, rate limiting, results rendering,
 *          badge logic, social sharing, leaderboard & gallery.
 */

'use strict';

/* ============================================================
   CONSTANTS
   ============================================================ */
const API_RATE  = '/api/RateAi/rateAiArt';
const API_BOARD = '/api/RateAi/getLeaderBoard';

const RATE_LIMIT_MAX      = 5;
const RATE_LIMIT_WINDOW   = 10 * 60 * 1000; // 10 minutes in ms
const LS_TIMESTAMPS_KEY   = 'rateAiArt_timestamps';

const BADGE_BRONZE_MIN = 7;
const BADGE_SILVER_MIN = 9;
const BADGE_GOLD_EXACT = 10;

const GAUGE_CIRCUMFERENCE = 2 * Math.PI * 80; // r=80

/* ============================================================
   STATE
   ============================================================ */
let currentBase64   = null;
let currentMimeType = null;
let currentResult   = null;   // EvaluationResponse from API
let currentArtUrl   = null;   // hosted URL after publishing
let countdownInterval = null;

/* ============================================================
   ELEMENT REFS (resolved lazily to avoid null on early load)
   ============================================================ */
const el = id => document.getElementById(id);

/* ============================================================
   1. IMAGE UPLOAD & DROPZONE
   ============================================================ */
function initDropzone() {
    const dropzone       = el('dropzone');
    const fileInput      = el('file-input');
    const imagePreview   = el('image-preview');
    const dropzoneContent = el('dropzone-content');
    const previewOverlay = el('preview-overlay');
    const fileError      = el('file-error');

    if (!dropzone) return;

    // Click → open file picker
    dropzone.addEventListener('click', e => {
        if (e.target === previewOverlay || previewOverlay.contains(e.target)) return;
        fileInput.click();
    });

    // Keyboard accessibility
    dropzone.addEventListener('keydown', e => {
        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); fileInput.click(); }
    });

    // Drag events
    dropzone.addEventListener('dragover', e => { e.preventDefault(); dropzone.classList.add('drag-over'); });
    dropzone.addEventListener('dragleave', () => dropzone.classList.remove('drag-over'));
    dropzone.addEventListener('drop', e => {
        e.preventDefault();
        dropzone.classList.remove('drag-over');
        const files = e.dataTransfer.files;
        if (files.length) processFile(files[0]);
    });

    // Change via file input
    fileInput.addEventListener('change', () => {
        if (fileInput.files.length) processFile(fileInput.files[0]);
        fileInput.value = '';
    });

    // Change image via overlay
    previewOverlay.addEventListener('click', e => { e.stopPropagation(); fileInput.click(); });

    function processFile(file) {
        fileError.style.display = 'none';

        // Validate type
        const allowed = ['image/png', 'image/jpeg', 'image/webp', 'image/gif'];
        if (!allowed.includes(file.type)) {
            showFileError('Please upload a PNG, JPG, WEBP, or GIF image.');
            return;
        }

        // Validate size (20 MB)
        if (file.size > 20 * 1024 * 1024) {
            showFileError('Image must be smaller than 20 MB.');
            return;
        }

        currentMimeType = file.type;

        const reader = new FileReader();
        reader.onload = evt => {
            const dataUrl = evt.target.result;
            // Extract pure base64 (strip data URI prefix)
            currentBase64 = dataUrl.split(',')[1];

            // Show preview
            imagePreview.src = dataUrl;
            imagePreview.classList.add('visible');
            dropzoneContent.style.display = 'none';
            dropzone.classList.add('has-image');

            enableSubmit();
        };
        reader.readAsDataURL(file);
    }

    function showFileError(msg) {
        fileError.textContent = msg;
        fileError.style.display = 'block';
    }
}

function enableSubmit() {
    const btn = el('submit-btn');
    if (!btn) return;
    const limited = isRateLimited();
    btn.disabled = !(currentBase64 && !limited);
}

/* ============================================================
   2. RATE LIMITING (client-side mirror)
   ============================================================ */
function getTimestamps() {
    try {
        return JSON.parse(localStorage.getItem(LS_TIMESTAMPS_KEY) || '[]');
    } catch { return []; }
}

function saveTimestamps(arr) {
    localStorage.setItem(LS_TIMESTAMPS_KEY, JSON.stringify(arr));
}

function getRecentTimestamps() {
    const now = Date.now();
    return getTimestamps().filter(t => now - t < RATE_LIMIT_WINDOW);
}

function recordSubmission() {
    const ts = getRecentTimestamps();
    ts.push(Date.now());
    saveTimestamps(ts);
}

function isRateLimited() {
    return getRecentTimestamps().length >= RATE_LIMIT_MAX;
}

/** Returns ms until the oldest submission in the window expires */
function msUntilNextSlot() {
    const ts = getRecentTimestamps().sort((a, b) => a - b);
    if (ts.length < RATE_LIMIT_MAX) return 0;
    return RATE_LIMIT_WINDOW - (Date.now() - ts[0]);
}

function formatCountdown(ms) {
    const totalSec = Math.max(0, Math.ceil(ms / 1000));
    const m = Math.floor(totalSec / 60).toString().padStart(2, '0');
    const s = (totalSec % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
}

function showRateLimitBanner() {
    el('rate-limit-banner').classList.add('visible');
    el('submit-btn').disabled = true;
    updateCountdown();
    clearInterval(countdownInterval);
    countdownInterval = setInterval(() => {
        if (!isRateLimited()) {
            clearInterval(countdownInterval);
            hideRateLimitBanner();
            enableSubmit();
        } else {
            updateCountdown();
        }
    }, 1000);
}

function updateCountdown() {
    const timer = el('countdown-timer');
    if (timer) timer.textContent = formatCountdown(msUntilNextSlot());
}

function hideRateLimitBanner() {
    el('rate-limit-banner').classList.remove('visible');
}

/* ============================================================
   3. FORM SUBMISSION
   ============================================================ */
function initForm() {
    const form = el('rate-form');
    if (!form) return;

    form.addEventListener('submit', async e => {
        e.preventDefault();
        if (!currentBase64) return;
        if (isRateLimited()) { showRateLimitBanner(); return; }

        const nickname = (el('nickname-input').value || '').trim() || null;
        const consent  = el('consent-checkbox').checked;

        setLoadingState(true);

        try {
            const response = await fetch(API_RATE, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    base64Image:            currentBase64,
                    mimeType:               currentMimeType,
                    showcaseResultAccepted: consent,
                    nickname:               nickname
                })
            });

            if (response.status === 429) {
                // Server-side rate limit hit — record client-side too
                recordSubmission(); // ensure local mirror is updated
                setLoadingState(false);
                showRateLimitBanner();
                return;
            }

            if (!response.ok) {
                throw new Error(`Server error: ${response.status}`);
            }

            const result = await response.json();
            recordSubmission();

            currentResult = result;
            currentArtUrl = null; // will only be available if server saved and we fetch leaderboard

            setLoadingState(false);
            renderResults(result);
            revealLeaderboardSection();
            await loadLeaderboard();

            // Check rate limit for next submission
            if (isRateLimited()) showRateLimitBanner();

        } catch (err) {
            setLoadingState(false);
            showGlobalError('Something went wrong. Please try again.');
            console.error(err);
        }
    });
}

function setLoadingState(isLoading) {
    const btn     = el('submit-btn');
    const overlay = el('loading-overlay');
    const form    = el('rate-form');

    if (isLoading) {
        btn.disabled = true;
        btn.style.display = 'none';
        overlay.classList.add('visible');
    } else {
        btn.style.display = 'inline-flex';
        overlay.classList.remove('visible');
        enableSubmit();
    }
}

function showGlobalError(msg) {
    // Reuse file-error element for API errors
    const errEl = el('file-error');
    if (errEl) {
        errEl.textContent = msg;
        errEl.style.display = 'block';
    }
}

/* ============================================================
   4. RESULTS RENDERING
   ============================================================ */
function renderResults(result) {
    const section = el('results-section');
    section.classList.add('visible');

    // Scroll to results
    section.scrollIntoView({ behavior: 'smooth', block: 'start' });

    const score = result.overallScore ?? 0;

    // Gauge
    renderGauge(score);

    // Sub-scores
    renderSubscores(result);

    // Badge
    renderBadge(score);

    // Tips
    renderTips(result.improvementTips ?? []);

    // Share buttons
    renderShareButtons(score);

    // Headline
    updateResultsHeadline(score);
}

function updateResultsHeadline(score) {
    const headline = el('results-headline');
    const subline  = el('results-subline');
    if (score >= 9.5)      { headline.textContent = '🌟 Outstanding!'; }
    else if (score >= 8)   { headline.textContent = '🎉 Impressive Work!'; }
    else if (score >= 7)   { headline.textContent = '✅ Good Result!'; }
    else if (score >= 5)   { headline.textContent = '📈 Room to Grow'; }
    else                   { headline.textContent = '📝 Here\'s Your Score'; }
    if (subline) subline.textContent = `Your overall score is ${score.toFixed(2)}/10.`;
}

function renderGauge(score) {
    const fill      = el('gauge-fill');
    const scoreEl   = el('gauge-score');
    const verdictEl = el('score-verdict');

    const pct = Math.min(score / 10, 1);
    const offset = GAUGE_CIRCUMFERENCE * (1 - pct);

    // Trigger animation after paint
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            fill.style.strokeDashoffset = offset;
        });
    });

    scoreEl.textContent = score.toFixed(1);

    if (score >= 9)      { verdictEl.textContent = 'Exceptional'; verdictEl.style.color = '#a78bfa'; }
    else if (score >= 7) { verdictEl.textContent = 'Great'; verdictEl.style.color = '#818cf8'; }
    else if (score >= 5) { verdictEl.textContent = 'Average'; verdictEl.style.color = '#64748b'; }
    else                 { verdictEl.textContent = 'Needs Work'; verdictEl.style.color = '#ef4444'; }
}

function renderSubscores(result) {
    const container = el('subscores-container');
    if (!container) return;

    const dimensions = [
        { label: 'Creativity',            value: result.creativity },
        { label: 'Complexity',            value: result.complexity },
        { label: 'Render Quality',        value: result.renderQuality },
        { label: 'Lighting & Colors',     value: result.lightingAndColors },
        { label: 'Composition',           value: result.composition },
        { label: 'Stylistic Consistency', value: result.stylisticConsistency },
    ];

    container.innerHTML = dimensions.map((d, i) => {
        const pct = ((d.value ?? 0) / 10 * 100).toFixed(1);
        return `
        <div class="subscore-item" style="animation-delay:${i * 80}ms">
            <div class="subscore-header">
                <span class="subscore-name">${escapeHtml(d.label)}</span>
                <span class="subscore-value">${(d.value ?? 0).toFixed(1)}</span>
            </div>
            <div class="subscore-track" role="progressbar" aria-label="${escapeHtml(d.label)}" aria-valuenow="${(d.value ?? 0).toFixed(1)}" aria-valuemin="0" aria-valuemax="10">
                <div class="subscore-bar" data-pct="${pct}"></div>
            </div>
        </div>`;
    }).join('');

    // Animate bars
    requestAnimationFrame(() => {
        requestAnimationFrame(() => {
            container.querySelectorAll('.subscore-bar').forEach(bar => {
                bar.style.width = bar.dataset.pct + '%';
            });
        });
    });
}

function renderBadge(score) {
    const display = el('badge-display');
    const img     = el('badge-img');
    const name    = el('badge-name');
    const desc    = el('badge-desc');

    let badge = null;

    if (score >= BADGE_GOLD_EXACT) {
        badge = { src: '/img/badges/gold.webp',   label: 'Gold Badge',   cls: 'gold',   tip: 'Perfect score! Legendary.' };
    } else if (score >= BADGE_SILVER_MIN) {
        badge = { src: '/img/badges/silver.webp', label: 'Silver Badge', cls: 'silver', tip: 'Near-perfect. Outstanding work!' };
    } else if (score >= BADGE_BRONZE_MIN) {
        badge = { src: '/img/badges/bronze.webp', label: 'Bronze Badge', cls: 'bronze', tip: 'Great start! Keep creating.' };
    }

    if (badge) {
        img.src          = badge.src;
        img.alt          = badge.label;
        name.textContent = badge.label;
        name.className   = `badge-name ${badge.cls}`;
        desc.textContent = badge.tip;
        display.classList.add('visible');
    } else {
        display.classList.remove('visible');
    }
}

function renderTips(tips) {
    const list = el('tips-list');
    if (!list) return;

    if (!tips || tips.length === 0) {
        list.innerHTML = '<li class="tip-item text-muted" style="font-size:.85rem;">No specific tips — your work is already great!</li>';
        return;
    }

    list.innerHTML = tips.map((tip, i) => `
        <li class="tip-item">
            <div class="tip-bullet" aria-hidden="true">${i + 1}</div>
            <span>${escapeHtml(tip)}</span>
        </li>`).join('');
}

/* ============================================================
   5. SOCIAL SHARING
   ============================================================ */
function renderShareButtons(score) {
    const container = el('share-buttons');
    if (!container) return;

    const appUrl    = window.location.href.split('?')[0];
    const shareUrl  = currentArtUrl || appUrl;
    const badgeEmoji = score >= 10 ? '🥇' : score >= 9 ? '🥈' : score >= 7 ? '🥉' : '🎨';
    const shareText = `${badgeEmoji} My AI art just scored ${score.toFixed(1)}/10 on RateAiArt! Check it out →`;

    const twitterUrl   = buildTwitterUrl(shareText, shareUrl);
    const linkedInUrl  = buildLinkedInUrl(shareUrl);
    const facebookUrl  = buildFacebookUrl(shareUrl);
    const pinterestUrl = buildPinterestUrl(shareUrl, shareText, currentArtUrl);

    container.innerHTML = `
        <a href="${twitterUrl}" target="_blank" rel="noopener noreferrer"
           class="share-btn share-btn-x" aria-label="Share on X (Twitter)">
            ${iconX()}
            <span>X / Twitter</span>
        </a>
        <a href="${linkedInUrl}" target="_blank" rel="noopener noreferrer"
           class="share-btn share-btn-linkedin" aria-label="Share on LinkedIn">
            ${iconLinkedIn()}
            <span>LinkedIn</span>
        </a>
        <a href="${facebookUrl}" target="_blank" rel="noopener noreferrer"
           class="share-btn share-btn-facebook" aria-label="Share on Facebook">
            ${iconFacebook()}
            <span>Facebook</span>
        </a>
        <a href="${pinterestUrl}" target="_blank" rel="noopener noreferrer"
           class="share-btn share-btn-pinterest" aria-label="Share on Pinterest">
            ${iconPinterest()}
            <span>Pinterest</span>
        </a>`;
}

function buildTwitterUrl(text, url) {
    return `https://x.com/intent/tweet?text=${encodeURIComponent(text + ' ' + url)}`;
}

function buildLinkedInUrl(url) {
    return `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(url)}`;
}

function buildFacebookUrl(url) {
    return `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`;
}

function buildPinterestUrl(url, description, mediaUrl) {
    let base = `https://pinterest.com/pin/create/button/?url=${encodeURIComponent(url)}&description=${encodeURIComponent(description)}`;
    if (mediaUrl) base += `&media=${encodeURIComponent(mediaUrl)}`;
    return base;
}

/* ── Social icons (inline SVG) ── */
function iconX() {
    return `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.744l7.73-8.835L1.254 2.25H8.08l4.253 5.622 5.911-5.622Zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>`;
}

function iconLinkedIn() {
    return `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/></svg>`;
}

function iconFacebook() {
    return `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/></svg>`;
}

function iconPinterest() {
    return `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 0C5.373 0 0 5.373 0 12c0 5.084 3.163 9.426 7.627 11.174-.105-.949-.2-2.405.042-3.441.218-.937 1.407-5.965 1.407-5.965s-.359-.719-.359-1.782c0-1.668.967-2.914 2.171-2.914 1.023 0 1.518.769 1.518 1.69 0 1.029-.655 2.568-.994 3.995-.283 1.194.599 2.169 1.777 2.169 2.133 0 3.772-2.249 3.772-5.495 0-2.873-2.064-4.882-5.012-4.882-3.414 0-5.418 2.561-5.418 5.207 0 1.031.397 2.138.893 2.738a.36.36 0 0 1 .083.345l-.333 1.36c-.053.22-.174.267-.402.161-1.499-.698-2.436-2.889-2.436-4.649 0-3.785 2.75-7.262 7.929-7.262 4.163 0 7.398 2.967 7.398 6.931 0 4.136-2.607 7.464-6.227 7.464-1.216 0-2.359-.632-2.75-1.378l-.748 2.853c-.271 1.043-1.002 2.35-1.492 3.146C9.57 23.812 10.763 24 12 24c6.627 0 12-5.373 12-12S18.627 0 12 0z"/></svg>`;
}

/* ============================================================
   6. LEADERBOARD & GALLERY
   ============================================================ */
function revealLeaderboardSection() {
    const lbSection = el('leaderboard-section');
    const gallerySection = el('gallery-section');
    if (lbSection) lbSection.classList.add('visible');
    if (gallerySection) gallerySection.style.display = 'block';
}

async function loadLeaderboard() {
    const loading = el('leaderboard-loading');
    const empty   = el('leaderboard-empty');
    const table   = el('leaderboard-table');
    const body    = el('leaderboard-body');
    const grid    = el('gallery-grid');

    if (loading) loading.style.display = 'block';
    if (empty)   empty.style.display   = 'none';
    if (table)   table.style.display   = 'none';

    try {
        const response = await fetch(`${API_BOARD}?limit=10`);
        if (!response.ok) throw new Error('Failed to fetch leaderboard');

        const entries = await response.json();

        if (loading) loading.style.display = 'none';

        if (!entries || entries.length === 0) {
            if (empty) empty.style.display = 'block';
            return;
        }

        // Render table
        if (table) table.style.display = 'table';
        if (body)  body.innerHTML = entries.map((entry, i) => renderLeaderboardRow(entry, i)).join('');

        // Render gallery
        const artEntries = entries.filter(e => e.artUrl);
        if (grid) {
            if (artEntries.length === 0) {
                grid.innerHTML = '<p class="text-muted" style="font-size:.9rem;">No artworks published yet.</p>';
            } else {
                grid.innerHTML = artEntries.map(e => renderGalleryItem(e)).join('');
                grid.querySelectorAll('.gallery-item').forEach(item => {
                    item.addEventListener('click', () => openLightbox(item.dataset));
                });
            }
        }

        // Try to resolve artUrl for the current session result (for share links)
        const consent  = el('consent-checkbox') ? el('consent-checkbox').checked : false;
        const nickname = el('nickname-input')   ? el('nickname-input').value.trim() : '';
        if (consent && currentResult && entries.length > 0) {
            // Best effort: last entry with matching score
            const match = entries.find(e =>
                e.artUrl &&
                Math.abs(e.leaderBoardRate - (currentResult.overallScore ?? 0)) < 0.01
            );
            if (match) {
                currentArtUrl = match.artUrl;
                // Re-render share buttons with real art URL
                renderShareButtons(currentResult.overallScore ?? 0);
            }
        }

    } catch (err) {
        console.error('Leaderboard error:', err);
        if (loading) loading.style.display = 'none';
        if (empty) {
            empty.textContent = 'Could not load leaderboard. Please try again.';
            empty.style.display = 'block';
        }
    }
}

function renderLeaderboardRow(entry, index) {
    const rank      = index + 1;
    const rankCell  = rankDisplay(rank);
    const nick      = escapeHtml(entry.publisher?.nickname || 'Anonymous');
    const initials  = nick.slice(0, 2).toUpperCase();
    const score     = (entry.leaderBoardRate ?? 0).toFixed(2);
    const thumbHtml = entry.artUrl
        ? `<img src="${escapeHtml(entry.artUrl)}" alt="${nick}'s artwork thumbnail" loading="lazy" />`
        : `<div class="thumb-placeholder" aria-label="No artwork available">🎨</div>`;

    return `
    <tr>
        <td class="rank-cell">${rankCell}</td>
        <td>
            <div class="publisher-cell">
                <div class="publisher-avatar" aria-hidden="true">${initials}</div>
                <div class="publisher-name">${nick}</div>
            </div>
        </td>
        <td class="score-cell" aria-label="Score: ${score}">${score}</td>
        <td class="thumb-cell">${thumbHtml}</td>
    </tr>`;
}

function rankDisplay(rank) {
    if (rank === 1) return '<span class="rank-medal-1" aria-label="Rank 1">🥇</span>';
    if (rank === 2) return '<span class="rank-medal-2" aria-label="Rank 2">🥈</span>';
    if (rank === 3) return '<span class="rank-medal-3" aria-label="Rank 3">🥉</span>';
    return `<span class="rank-number" aria-label="Rank ${rank}">${rank}</span>`;
}

function renderGalleryItem(entry) {
    const nick  = escapeHtml(entry.publisher?.nickname || 'Anonymous');
    const score = (entry.leaderBoardRate ?? 0).toFixed(2);
    const url   = escapeHtml(entry.artUrl);

    return `
    <div class="gallery-item" role="button" tabindex="0" aria-label="View ${nick}'s artwork, score ${score}"
         data-url="${url}" data-nick="${nick}" data-score="${score}">
        <img src="${url}" alt="${nick}'s AI artwork" loading="lazy" />
        <div class="gallery-item-overlay" aria-hidden="true">
            <div class="gallery-item-nick">${nick}</div>
            <div class="gallery-item-score">⭐ ${score}</div>
        </div>
    </div>`;
}

async function refreshLeaderboard() {
    const btn = el('refresh-leaderboard-btn');
    if (btn) { btn.disabled = true; btn.textContent = '↻ Loading…'; }
    await loadLeaderboard();
    if (btn) { btn.disabled = false; btn.textContent = '↻ Refresh'; }
}

/* ============================================================
   7. LIGHTBOX
   ============================================================ */
function initLightbox() {
    const backdrop = el('lightbox');
    const closeBtn = el('lightbox-close');

    if (!backdrop) return;

    closeBtn.addEventListener('click', closeLightbox);
    backdrop.addEventListener('click', e => { if (e.target === backdrop) closeLightbox(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeLightbox(); });
}

function openLightbox({ url, nick, score }) {
    const backdrop = el('lightbox');
    const img      = el('lightbox-img');
    const nickEl   = el('lightbox-nick');
    const scoreEl  = el('lightbox-score');

    img.src          = url;
    img.alt          = `${nick}'s AI artwork`;
    nickEl.textContent  = nick;
    scoreEl.textContent = `Score: ${score} / 10`;

    backdrop.classList.add('visible');
    backdrop.setAttribute('aria-hidden', 'false');
    backdrop.focus();
}

function closeLightbox() {
    const backdrop = el('lightbox');
    backdrop.classList.remove('visible');
    backdrop.setAttribute('aria-hidden', 'true');
}

/* ============================================================
   8. RESET FORM
   ============================================================ */
function resetForm() {
    const dropzone       = el('dropzone');
    const fileInput      = el('file-input');
    const imagePreview   = el('image-preview');
    const dropzoneContent = el('dropzone-content');
    const fileError      = el('file-error');

    currentBase64   = null;
    currentMimeType = null;
    currentResult   = null;
    currentArtUrl   = null;

    if (imagePreview) { imagePreview.src = ''; imagePreview.classList.remove('visible'); }
    if (dropzoneContent) dropzoneContent.style.display = '';
    if (dropzone) dropzone.classList.remove('has-image', 'drag-over');
    if (fileInput) fileInput.value = '';
    if (fileError) fileError.style.display = 'none';
    if (el('nickname-input')) el('nickname-input').value = '';
    if (el('consent-checkbox')) el('consent-checkbox').checked = false;

    el('results-section').classList.remove('visible');

    enableSubmit();

    el('rate-section').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

/* ============================================================
   9. INTERSECTION OBSERVER (scroll animations)
   ============================================================ */
function initScrollAnimations() {
    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('in-view');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.12 });

    document.querySelectorAll('.animate-fade-up').forEach(el => observer.observe(el));
}

/* ============================================================
   10. HELPERS
   ============================================================ */
function escapeHtml(str) {
    if (typeof str !== 'string') return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

/* ============================================================
   11. BOOT
   ============================================================ */
document.addEventListener('DOMContentLoaded', () => {
    initDropzone();
    initForm();
    initLightbox();
    initScrollAnimations();

    // If already rate-limited on page load, show the banner immediately
    if (isRateLimited()) {
        showRateLimitBanner();
    }
});
