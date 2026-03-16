/**
 * AI Career Advisor - Chat with History
 */
(function () {
    'use strict';

    var currentSessionId = null;
    var isProcessing = false;
    var lastAiData = null;
    var showingHistory = false;
    var SESSION_KEY = 'ai_chat_session_id';

    var bubble, panel, messagesContainer, inputField, sendBtn, uploadBtn, uploadInput;

    var bubbleSvgIcon = '<svg class="ai-bubble-icon" viewBox="0 0 64 64" width="30" height="30" fill="none">' +
        '<rect x="12" y="28" width="40" height="24" rx="4" fill="white" opacity="0.95"/>' +
        '<rect x="22" y="20" width="20" height="10" rx="3" fill="none" stroke="white" stroke-width="2.5"/>' +
        '<rect x="26" y="36" width="12" height="6" rx="2" fill="rgba(79,70,229,0.5)"/>' +
        '<path d="M42 8 Q54 8 54 15 Q54 22 42 22 L37 22 L34 27 L34 22 Q24 22 24 15 Q24 8 34 8 Z" fill="white" opacity="0.9"/>' +
        '<circle cx="33" cy="15" r="1.3" fill="rgba(79,70,229,0.8)"/>' +
        '<circle cx="38" cy="15" r="1.3" fill="rgba(79,70,229,0.8)"/>' +
        '<circle cx="43" cy="15" r="1.3" fill="rgba(79,70,229,0.8)"/></svg>';

    document.addEventListener('DOMContentLoaded', function () {
        bubble = document.getElementById('aiChatBubble');
        panel = document.getElementById('aiChatPanel');
        messagesContainer = document.getElementById('aiChatMessages');
        inputField = document.getElementById('aiChatInput');
        sendBtn = document.getElementById('aiChatSendBtn');
        uploadBtn = document.getElementById('aiChatUploadBtn');
        uploadInput = document.getElementById('aiChatUploadInput');

        if (!bubble || !panel || !messagesContainer || !inputField || !sendBtn || !uploadBtn || !uploadInput) {
            return;
        }

        bubble.addEventListener('click', togglePanel);
        sendBtn.addEventListener('click', sendMessage);
        uploadBtn.addEventListener('click', function () { uploadInput.click(); });
        uploadInput.addEventListener('change', handleFileUpload);

        inputField.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        inputField.addEventListener('input', function () {
            this.style.height = 'auto';
            this.style.height = Math.min(this.scrollHeight, 80) + 'px';
        });

        var closeBtn = document.getElementById('aiChatCloseBtn');
        if (closeBtn) closeBtn.addEventListener('click', togglePanel);

        var newBtn = document.getElementById('aiChatNewBtn');
        if (newBtn) newBtn.addEventListener('click', startNewChat);

        var historyBtn = document.getElementById('aiChatHistoryBtn');
        if (historyBtn) historyBtn.addEventListener('click', toggleHistory);

        var labelCloseBtn = document.getElementById('aiBubbleLabelClose');
        if (labelCloseBtn) {
            labelCloseBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                var label = document.getElementById('aiBubbleLabel');
                if (label) label.classList.add('hidden');
            });
        }

        var savedSession = localStorage.getItem(SESSION_KEY);
        if (savedSession && isAuthenticated()) {
            currentSessionId = parseInt(savedSession, 10);
        }
    });

    function togglePanel() {
        var isOpen = panel.classList.toggle('open');
        bubble.classList.toggle('active', isOpen);
        bubble.innerHTML = isOpen ? '<i class="fas fa-times" style="font-size:20px"></i>' : bubbleSvgIcon;

        var label = document.getElementById('aiBubbleLabel');
        if (label && isOpen) label.classList.add('hidden');

        if (isOpen) {
            inputField.focus();
            if (currentSessionId && messagesContainer.querySelector('.ai-welcome')) {
                loadSession(currentSessionId);
            }
        }
    }

    function toggleHistory() {
        showingHistory = !showingHistory;
        var historyPanel = document.getElementById('aiHistoryPanel');
        var historyBtn = document.getElementById('aiChatHistoryBtn');

        if (!historyPanel || !historyBtn) return;

        if (showingHistory) {
            historyPanel.classList.add('show');
            messagesContainer.style.display = 'none';
            historyBtn.classList.add('active-view');
            loadHistory();
        } else {
            historyPanel.classList.remove('show');
            messagesContainer.style.display = 'flex';
            historyBtn.classList.remove('active-view');
        }
    }

    function loadHistory() {
        if (!isAuthenticated()) return;

        var listEl = document.getElementById('aiHistoryList');
        if (!listEl) return;

        listEl.innerHTML = '<div style="text-align:center;padding:20px;color:#9ca3af;font-size:13px;"><i class="fas fa-spinner fa-spin"></i> Dang tai...</div>';

        fetch('/AiAdvisor/Sessions', { headers: { 'RequestVerificationToken': getAntiForgeryToken() } })
            .then(function (r) { return r.json(); })
            .then(function (sessions) {
                if (!Array.isArray(sessions) || sessions.length === 0) {
                    listEl.innerHTML = '<div class="ai-history-empty"><i class="fas fa-clock-rotate-left" style="font-size:28px;display:block;margin-bottom:8px;color:#d1d5db;"></i>Chua co lich su tro chuyen</div>';
                    return;
                }

                var html = '';
                sessions.forEach(function (s) {
                    var date = new Date(s.createdAt);
                    var dateStr = date.toLocaleDateString('vi-VN') + ' ' +
                        date.getHours().toString().padStart(2, '0') + ':' +
                        date.getMinutes().toString().padStart(2, '0');

                    html += '<div class="ai-history-item" data-session-id="' + s.id + '">' +
                        '<div class="ai-history-item-title">' + escapeHtml(s.title) + '</div>' +
                        '<div class="ai-history-item-meta">' + dateStr + ' &middot; ' + s.messageCount + ' tin nhan</div>' +
                        '</div>';
                });

                listEl.innerHTML = html;
                listEl.querySelectorAll('.ai-history-item').forEach(function (item) {
                    item.addEventListener('click', function () {
                        loadSession(parseInt(this.dataset.sessionId, 10));
                    });
                });
            })
            .catch(function () {
                listEl.innerHTML = '<div class="ai-history-empty">Loi tai lich su</div>';
            });
    }

    function loadSession(sessionId) {
        var historyPanel = document.getElementById('aiHistoryPanel');
        var historyBtn = document.getElementById('aiChatHistoryBtn');

        currentSessionId = sessionId;
        showingHistory = false;
        if (historyPanel) historyPanel.classList.remove('show');
        if (historyBtn) historyBtn.classList.remove('active-view');

        messagesContainer.style.display = 'flex';
        messagesContainer.innerHTML = '<div style="text-align:center;padding:20px;color:#9ca3af;font-size:13px;"><i class="fas fa-spinner fa-spin"></i> Dang tai...</div>';

        fetch('/AiAdvisor/History?sessionId=' + sessionId, { headers: { 'RequestVerificationToken': getAntiForgeryToken() } })
            .then(readJsonResponse)
            .then(function (messages) {
                if (!Array.isArray(messages)) {
                    if (isMissingSessionResponse(messages)) {
                        resetCurrentSession();
                        messagesContainer.innerHTML = getWelcomeHtml();
                        addMessage('assistant', getResponseMessage(messages, 'Phien chat cu khong con ton tai. Da tao phien moi cho ban.'));
                        return;
                    }

                    throw new Error('INVALID_HISTORY');
                }

                messagesContainer.innerHTML = '';
                messages.forEach(function (m) {
                    if (m.jsonData && m.role === 'assistant') {
                        try {
                            renderAiResponse(JSON.parse(m.jsonData), null);
                        } catch (e) {
                            addMessage('assistant', m.content);
                        }
                    } else {
                        addMessage(m.role, m.content);
                    }
                });
                scrollToBottom();
            })
            .catch(function () {
                messagesContainer.innerHTML = '';
                addMessage('assistant', 'Loi khi tai lich su. Vui long thu lai.');
            });
    }

    function startNewChat() {
        resetCurrentSession();

        var historyPanel = document.getElementById('aiHistoryPanel');
        var historyBtn = document.getElementById('aiChatHistoryBtn');
        if (showingHistory) {
            showingHistory = false;
            if (historyPanel) historyPanel.classList.remove('show');
            if (historyBtn) historyBtn.classList.remove('active-view');
            messagesContainer.style.display = 'flex';
        }

        messagesContainer.innerHTML = getWelcomeHtml();
    }

    function sendMessage() {
        var message = inputField.value.trim();
        if (!message || isProcessing) return;
        if (!isAuthenticated()) {
            showLoginRequired();
            return;
        }

        var historyPanel = document.getElementById('aiHistoryPanel');
        var historyBtn = document.getElementById('aiChatHistoryBtn');
        if (showingHistory) {
            showingHistory = false;
            if (historyPanel) historyPanel.classList.remove('show');
            if (historyBtn) historyBtn.classList.remove('active-view');
            messagesContainer.style.display = 'flex';
        }

        addMessage('user', message);
        inputField.value = '';
        inputField.style.height = 'auto';
        showTyping();
        isProcessing = true;
        updateBtns();

        fetch('/AiAdvisor/Chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ sessionId: currentSessionId, message: message })
        })
            .then(readJsonResponse)
            .then(function (data) {
                hideTyping();
                isProcessing = false;
                updateBtns();

                if (data.sessionId) {
                    currentSessionId = data.sessionId;
                    localStorage.setItem(SESSION_KEY, data.sessionId);
                }

                if (data.success && data.data) {
                    lastAiData = data;
                    renderAiResponse(data.data, data.matchedJobs);
                    return;
                }

                if (isMissingSessionResponse(data)) {
                    resetCurrentSession();
                }

                addMessage('assistant', getResponseMessage(data, 'Xin loi, da xay ra loi.'));
            })
            .catch(function (err) {
                hideTyping();
                isProcessing = false;
                updateBtns();

                if (err.message === 'LOGIN') {
                    showLoginRequired();
                } else if (err.message === 'SERVER') {
                    addMessage('assistant', 'May chu dang tra ve du lieu khong hop le. Vui long thu lai.');
                } else {
                    addMessage('assistant', 'Loi ket noi. Vui long thu lai.');
                }
            });
    }

    function handleFileUpload() {
        var file = uploadInput.files[0];
        if (!file) return;
        if (!isAuthenticated()) {
            showLoginRequired();
            uploadInput.value = '';
            return;
        }

        var ext = file.name.split('.').pop().toLowerCase();
        if (ext !== 'pdf' && ext !== 'docx') {
            addMessage('assistant', 'Chi ho tro PDF hoac DOCX.');
            uploadInput.value = '';
            return;
        }

        if (file.size > 10 * 1024 * 1024) {
            addMessage('assistant', 'File qua lon (toi da 10MB).');
            uploadInput.value = '';
            return;
        }

        addMessage('user', 'Da tai len CV: ' + file.name);
        showTyping();
        isProcessing = true;
        updateBtns();

        var fd = new FormData();
        fd.append('file', file);
        if (currentSessionId) fd.append('sessionId', currentSessionId);

        fetch('/AiAdvisor/UploadCv', {
            method: 'POST',
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            body: fd
        })
            .then(readJsonResponse)
            .then(function (data) {
                hideTyping();
                isProcessing = false;
                updateBtns();
                uploadInput.value = '';

                if (data.sessionId) {
                    currentSessionId = data.sessionId;
                    localStorage.setItem(SESSION_KEY, data.sessionId);
                }

                if (data.success && data.data) {
                    lastAiData = data;
                    renderAiResponse(data.data, data.matchedJobs);
                    return;
                }

                if (isMissingSessionResponse(data)) {
                    resetCurrentSession();
                }

                addMessage('assistant', getResponseMessage(data, 'Loi khi phan tich CV.'));
            })
            .catch(function (err) {
                hideTyping();
                isProcessing = false;
                updateBtns();
                uploadInput.value = '';

                if (err.message === 'LOGIN') {
                    showLoginRequired();
                } else if (err.message === 'SERVER') {
                    addMessage('assistant', 'May chu dang tra ve du lieu khong hop le khi tai CV.');
                } else {
                    addMessage('assistant', 'Loi ket noi khi tai CV.');
                }
            });
    }

    function addMessage(role, content) {
        var welcome = messagesContainer.querySelector('.ai-welcome');
        var loginRequired = messagesContainer.querySelector('.ai-login-required');
        if (welcome) welcome.remove();
        if (loginRequired) loginRequired.remove();

        var now = new Date();
        var time = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');
        var item = document.createElement('div');
        item.className = 'ai-msg ' + role;
        item.innerHTML = '<div class="ai-msg-content">' +
            (role === 'assistant' ? formatText(content) : escapeHtml(content)) +
            '</div><div class="ai-msg-time">' + time + '</div>';
        messagesContainer.appendChild(item);
        scrollToBottom();
    }

    function renderAiResponse(data, matchedJobs) {
        var welcome = messagesContainer.querySelector('.ai-welcome');
        if (welcome) welcome.remove();

        var now = new Date();
        var time = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');

        var noResults = (!data.ky_nang_can_bo_sung || data.ky_nang_can_bo_sung.length === 0) &&
            (!data.goi_y_cong_viec || data.goi_y_cong_viec.length === 0) &&
            (!matchedJobs || matchedJobs.length === 0);

        if (noResults && data.danh_gia_chung) {
            var plainItem = document.createElement('div');
            plainItem.className = 'ai-msg assistant';
            plainItem.innerHTML = '<div class="ai-msg-content">' + formatText(data.danh_gia_chung) + '</div><div class="ai-msg-time">' + time + '</div>';
            messagesContainer.appendChild(plainItem);
            scrollToBottom();
            return;
        }

        var item = document.createElement('div');
        item.className = 'ai-msg assistant';
        var html = '';

        if (data.danh_gia_chung) {
            html += '<div class="ai-result-card"><div class="ai-result-card-header evaluation"><i class="fas fa-user-tie"></i> Danh gia</div><div class="ai-result-card-body">' + formatText(data.danh_gia_chung) + '</div></div>';
        }

        if (data.ky_nang_can_bo_sung && data.ky_nang_can_bo_sung.length > 0) {
            html += '<div class="ai-result-card"><div class="ai-result-card-header skills"><i class="fas fa-chart-line"></i> Ky nang can bo sung</div><div class="ai-result-card-body">';
            data.ky_nang_can_bo_sung.forEach(function (s) {
                html += '<div class="ai-skill-item"><div class="ai-skill-name">' + escapeHtml(s.ten_ky_nang) + '</div><div class="ai-skill-reason">' + escapeHtml(s.ly_do) + '</div><div class="ai-skill-course"><i class="fas fa-graduation-cap"></i>' + escapeHtml(s.goi_y_khoa_hoc) + '</div></div>';
            });
            html += '</div></div>';
        }

        if (data.goi_y_cong_viec && data.goi_y_cong_viec.length > 0) {
            html += '<div class="ai-result-card"><div class="ai-result-card-header jobs"><i class="fas fa-briefcase"></i> Goi y viec lam</div><div class="ai-result-card-body">';
            data.goi_y_cong_viec.forEach(function (j) {
                html += '<div class="ai-job-item"><div class="ai-job-info"><div class="ai-job-title">' + escapeHtml(j.chuc_danh) + '</div><div class="ai-job-salary">' + escapeHtml(j.muc_luong_du_kien) + '</div><div class="ai-job-reason">' + escapeHtml(j.ly_do_phu_hop) + '</div></div>';
                if (j.job_url) html += '<div class="ai-job-action"><a href="' + j.job_url + '" class="btn btn-success btn-sm" target="_blank">Xem</a></div>';
                html += '</div>';
            });
            html += '</div></div>';
        }

        if (matchedJobs && matchedJobs.length > 0) {
            html += '<div class="ai-result-card"><div class="ai-result-card-header ai-matched-jobs-header"><i class="fas fa-search"></i> Viec lam phu hop tren JobPortal</div><div class="ai-result-card-body">';
            matchedJobs.forEach(function (j) {
                var salary = '';
                if (j.salaryMin && j.salaryMax) salary = formatSalary(j.salaryMin) + ' - ' + formatSalary(j.salaryMax);
                else if (j.salaryMin) salary = 'Tu ' + formatSalary(j.salaryMin);
                else if (j.salaryMax) salary = 'Toi ' + formatSalary(j.salaryMax);

                html += '<div class="ai-matched-job-item"><div class="d-flex justify-content-between align-items-start"><div><div class="ai-matched-job-title">' + escapeHtml(j.title) + '</div><div class="ai-matched-job-company"><i class="fas fa-building me-1"></i>' + escapeHtml(j.companyName) + '</div><div class="ai-matched-job-meta">' +
                    (j.location ? '<i class="fas fa-map-marker-alt me-1"></i>' + escapeHtml(j.location) + ' ' : '') +
                    (salary ? '<i class="fas fa-money-bill-wave me-1"></i>' + salary : '') +
                    '</div></div><a href="' + j.url + '" class="btn btn-primary btn-sm" style="border-radius:6px;font-size:11px;" target="_blank">Xem</a></div></div>';
            });
            html += '</div></div>';
        }

        var hasResults = (data.ky_nang_can_bo_sung && data.ky_nang_can_bo_sung.length > 0) ||
            (data.goi_y_cong_viec && data.goi_y_cong_viec.length > 0);
        if (hasResults) {
            html += '<button class="ai-save-roadmap-btn" onclick="window.aiAdvisorSaveRoadmap()"><i class="fas fa-bookmark me-1"></i>Luu lo trinh</button>';
        }

        item.innerHTML = html + '<div class="ai-msg-time">' + time + '</div>';
        messagesContainer.appendChild(item);
        scrollToBottom();
    }

    window.aiAdvisorSaveRoadmap = function () {
        if (!lastAiData || !lastAiData.data) return;

        var btn = messagesContainer.querySelector('.ai-save-roadmap-btn:last-of-type');
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Dang luu...';
        }

        fetch('/AiAdvisor/SaveRoadmap', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                sessionId: currentSessionId,
                danhGiaChung: lastAiData.data.danh_gia_chung,
                kyNangCanBoSung: JSON.stringify(lastAiData.data.ky_nang_can_bo_sung),
                tuKhoaMoRong: JSON.stringify(lastAiData.data.tu_khoa_mo_rong),
                goiYCongViec: JSON.stringify(lastAiData.data.goi_y_cong_viec)
            })
        })
            .then(function (r) {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                return r.json();
            })
            .then(function (data) {
                if (!btn) return;

                if (data.success) {
                    btn.innerHTML = '<i class="fas fa-check me-1"></i>Da luu!';
                    btn.style.background = '#059669';
                } else {
                    btn.disabled = false;
                    btn.innerHTML = '<i class="fas fa-bookmark me-1"></i>Luu lo trinh';
                    alert(data.message || 'Loi luu lo trinh');
                }
            })
            .catch(function (err) {
                console.error('SaveRoadmap error:', err);
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = '<i class="fas fa-bookmark me-1"></i>Luu lo trinh';
                }
            });
    };

    function showTyping() {
        var el = document.createElement('div');
        el.className = 'ai-typing';
        el.id = 'aiTypingIndicator';
        el.innerHTML = '<div class="ai-typing-dot"></div><div class="ai-typing-dot"></div><div class="ai-typing-dot"></div>';
        messagesContainer.appendChild(el);
        scrollToBottom();
    }

    function hideTyping() {
        var el = document.getElementById('aiTypingIndicator');
        if (el) el.remove();
    }

    function updateBtns() {
        if (sendBtn) sendBtn.disabled = isProcessing;
        if (uploadBtn) uploadBtn.disabled = isProcessing;
    }

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    function showLoginRequired() {
        messagesContainer.innerHTML = '<div class="ai-login-required"><i class="fas fa-lock"></i><p>Vui long dang nhap de su dung<br>tinh nang tu van viec lam</p><a href="/Client/Account/Login" class="btn btn-primary"><i class="fas fa-sign-in-alt me-1"></i>Dang nhap</a></div>';
    }

    function isAuthenticated() {
        return document.body.dataset.authenticated === 'true';
    }

    function resetCurrentSession() {
        currentSessionId = null;
        lastAiData = null;
        localStorage.removeItem(SESSION_KEY);
    }

    function getAntiForgeryToken() {
        var aiForm = document.getElementById('aiAntiForgeryForm');
        if (aiForm) {
            var token = aiForm.querySelector('input[name="__RequestVerificationToken"]');
            if (token) return token.value;
        }

        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function readJsonResponse(r) {
        if (r.status === 401) throw new Error('LOGIN');

        return r.text().then(function (text) {
            if (!text) return {};

            try {
                var data = JSON.parse(text);
                if (data && typeof data === 'object') {
                    data._httpStatus = r.status;
                }
                return data;
            } catch (e) {
                console.error('AI advisor non-JSON response:', text);
                throw new Error('SERVER');
            }
        });
    }

    function getResponseMessage(data, fallback) {
        if (!data || typeof data !== 'object') return fallback;
        return data.message || data.error || data.title || fallback;
    }

    function isMissingSessionResponse(data) {
        var message = getResponseMessage(data, '').toLowerCase();
        return message.indexOf('phien chat') >= 0 && message.indexOf('khong ton tai') >= 0;
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function formatText(text) {
        if (!text) return '';
        return escapeHtml(text).replace(/\n/g, '<br>');
    }

    function formatSalary(amount) {
        if (!amount) return '';
        var n = parseFloat(amount);
        return n >= 1000000 ? (n / 1000000).toFixed(0) + ' tr' : n.toLocaleString('vi-VN') + ' d';
    }

    function getWelcomeHtml() {
        return '<div class="ai-welcome"><div class="ai-welcome-icon"><svg viewBox="0 0 36 36" width="24" height="24" fill="none"><rect x="6" y="14" width="24" height="16" rx="3" fill="white" opacity="0.95"/><rect x="12" y="9" width="12" height="7" rx="2" fill="none" stroke="white" stroke-width="2"/><rect x="14" y="20" width="8" height="4" rx="1.5" fill="rgba(255,255,255,0.5)"/></svg></div><h6>Xin chao!</h6><p>Toi se giup ban tim viec lam phu hop. Hay cho toi biet ban dang tim viec gi, hoac tai CV len de bat dau.</p></div>';
    }
})();
