/**********************
 * signalr-actions.js
 * 只做：按鈕與鍵盤事件，呼叫 invoke
 **********************/

(function () {
    const $ = (id) => document.getElementById(id);

    function ensureReady() {
        if (!window.chatApp || !window.chatApp.connection || !window.chatApp.ui) {
            console.error("chatApp 尚未初始化：請確認 signalr-conn.js 有先載入");
            return false;
        }
        return true;
    }

    async function sendMessage() {
        if (!ensureReady()) {
            console.log('[sendMessage] ensureReady failed');
            return;
        }

        const { connection, ui } = window.chatApp;

        //const userId = window.UserId;
        const userId = Number(window.UserId);
        const userName = window.UserName;
        const message = (ui.message?.value || "").trim();

        if (!Number.isInteger(userId) || userId <= 0) {
            console.log('[sendMessage] ❌ invalid userId:', window.UserId);
            return;
        }

        console.log('[sendMessage] values:', {
            userId,
            userName,
            message
        });

        if (!userId) {
            console.log('[sendMessage] ❌ userId missing');
            return;
        }
        if (!userName) {
            console.log('[sendMessage] ❌ userName missing');
            return;
        }
        if (!message) {
            console.log('[sendMessage] ❌ message missing');
            return;
        }

        await connection.invoke("SendMessage", userId, userName, message);

        ui.message.value = "";
        ui.message.focus();
    }



    async function analyzeText() {
        if (!ensureReady()) return;

        const { connection, ui } = window.chatApp;
        const text = (ui.message?.value || "").trim();
        if (!text) return;

        try {
            await connection.invoke("AnalyzeText", text);
        } catch (err) {
            console.error(err);
            alert("分析失敗，請確認已連線");
        }
    }

    function wireUI() {
        if (!ensureReady()) return;
        const { ui } = window.chatApp;

        // 預設名字
        if (ui.user && !ui.user.value.trim()) {
            ui.user.value = "User000";
        }

        // 送出按鈕
        const sendBtn = document.getElementById("sendBtn");
        if (sendBtn) {
            sendBtn.addEventListener("click", sendMessage);
        }

        // Enter 送出
        if (ui.message) {
            ui.message.addEventListener("keydown", (e) => {
                if (e.key === "Enter") sendMessage();
            });
        }

        // 分析按鈕
        const analyzeBtn = $("analyzeBtn");
        if (analyzeBtn) analyzeBtn.addEventListener("click", analyzeText);

        // 如果你想保留全域給 inline onclick（不建議，但可）
        window.sendMessage = sendMessage;
        window.analyzeText = analyzeText;

        const showNoteBtn = document.getElementById("showNote");
        if (showNoteBtn) {
            showNoteBtn.onclick = () => {
                const box = document.getElementById("note");
                if (!box || !latestAnalyzeResult) return;

                const notes =
                    latestAnalyzeResult.notes_zh ??
                    latestAnalyzeResult.notes_Zh ??
                    [];

                if (!notes.length) {
                    box.textContent = "（沒有學習提示）";
                    return;
                }

                box.innerHTML = notes.map(n => `• ${escapeHtml(n)}`).join("<br>");
            };
        }

        // ✅ Show Answer
        const showAnsBtn = document.getElementById("showAns");
        if (showAnsBtn) {
            showAnsBtn.onclick = () => {
                const box = document.getElementById("note");
                if (!box || !latestAnalyzeResult) return;

                const compose = latestAnalyzeResult.compose ?? {};
                const ans =
                    compose.ko_natural ??
                    compose.ko_Natural ??
                    null;

                box.textContent = ans || "（沒有自然韓語答案）";
            };
        }
        
    }

    function escapeHtml(s) {
        return String(s ?? "").replace(/[&<>"']/g, m => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            '"': "&quot;",
            "'": "&#39;"
        }[m]));
    }

    (function () {
      const toggleBtn = document.getElementById("sidebarToggle");
      const sidebar = document.getElementById("sidebar");
      const backdrop = document.getElementById("backdrop");

      function setOpen(isOpen) {
        sidebar.classList.toggle("open", isOpen);
        backdrop.hidden = !isOpen;
        toggleBtn.setAttribute("aria-expanded", String(isOpen));
        sidebar.setAttribute("aria-hidden", String(!isOpen));
        document.body.classList.toggle("noScroll", isOpen);
      }

      toggleBtn?.addEventListener("click", () => {
        const isOpen = sidebar.classList.contains("open");
        setOpen(!isOpen);
      });

      backdrop?.addEventListener("click", () => setOpen(false));

      // ESC 關閉
      window.addEventListener("keydown", (e) => {
        if (e.key === "Escape") setOpen(false);
      });
    })();

    // boot
    document.addEventListener("DOMContentLoaded", wireUI);
})();

(function () {
    const toggleBtn = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");
    const backdrop = document.getElementById("backdrop");

    function setOpen(isOpen) {
        sidebar.classList.toggle("open", isOpen);
        backdrop.hidden = !isOpen;
        toggleBtn.setAttribute("aria-expanded", String(isOpen));
        sidebar.setAttribute("aria-hidden", String(!isOpen));
        document.body.classList.toggle("noScroll", isOpen);
    }

    toggleBtn?.addEventListener("click", () => {
        const isOpen = sidebar.classList.contains("open");
        setOpen(!isOpen);
    });

    backdrop?.addEventListener("click", () => setOpen(false));

    // ESC 關閉
    window.addEventListener("keydown", (e) => {
        if (e.key === "Escape") setOpen(false);
    });
})();
