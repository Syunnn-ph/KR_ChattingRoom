// 你原本的 [] 標記（保留當 debug）
function showBracketMarking(input, errors) {
    let marked = input ?? "";
    const sorted = (errors || []).slice().sort((a, b) => b.start - a.start);

    for (const e of sorted) {
        marked =
            marked.slice(0, e.start) +
            "[" +
            marked.slice(e.start, e.end) +
            "]" +
            marked.slice(e.end);
    }
    //if (ui.marked) ui.marked.textContent = marked;
    console.log(marked);
}

function normalizeErrorsByOriginal(input, errors) {
    if (!input) return [];

    const arr = (errors || [])
        .filter(e => e && typeof e.original === "string" && e.original.length > 0)
        .map(e => ({ ...e }));

    // 依照模型給的 start 排序，幫我們從前往後找，避免重複命中同一段
    arr.sort((a, b) => (a.start ?? 0) - (b.start ?? 0));

    let searchFrom = 0;

    for (const e of arr) {
        // 如果模型給的 start/end 可信（切出來剛好等於 original），就保留
        const s = Number.isFinite(e.start) ? e.start : -1;
        const t = Number.isFinite(e.end) ? e.end : -1;

        if (s >= 0 && t > s) {
            const sliced = input.slice(s, t);
            if (sliced === e.original) {
                searchFrom = Math.max(searchFrom, t);
                continue;
            }
        }

        // 否則：用 original 重新找位置（從 searchFrom 開始找）
        let idx = input.indexOf(e.original, searchFrom);

        // 找不到就退一步：從 0 找（避免因為 searchFrom 卡死）
        if (idx === -1) idx = input.indexOf(e.original);

        if (idx !== -1) {
            e.start = idx;
            e.end = idx + e.original.length;
            searchFrom = Math.max(searchFrom, e.end);
        } else {
            // 真的找不到：這筆不要畫（不然一定歪）
            e._skip = true;
        }
    }

    return arr.filter(e => !e._skip && Number.isFinite(e.start) && Number.isFinite(e.end) && e.end > e.start);
}

function renderZhKoPairs(data) {
    const box = document.getElementById("mapBox");
    if (!box) return;

    box.innerHTML = ""; // 清空

    const units = data?.units || [];
    for (const u of units) {
        if (!u?.text) continue;

        // 你要的顯示：text : ko
        const left = u.text;
        const right = u.ko ?? "(無對應)";

        const line = document.createElement("div");
        line.textContent = `${left}: ${right}`;
        box.appendChild(line);
    }
}



/**********************
 * Grammar Visualize
 **********************/
function renderGrammarViz(input, errors, container) {
    if (!container) return;
    container.innerHTML = "";
    if (!input) return;

    const sorted = normalizeErrorsByOriginal(input, errors)
        .slice()
        .sort((a, b) => a.start - b.start);

    // 重疊保護
    const normalized = [];
    for (const e of sorted) {
        const last = normalized[normalized.length - 1];
        if (!last) { normalized.push(e); continue; }

        if (e.original && e.original.length < (e.end - e.start)) {
            const lastLen = last.end - last.start;
            const eLen = e.end - e.start;
            if (eLen < lastLen) normalized[normalized.length - 1] = e;
        } else {
            normalized.push(e);
        }
    }

    let cursor = 0;

    for (const e of normalized) {
        if (cursor < e.start) {
            container.append(document.createTextNode(input.slice(cursor, e.start)));
        }

        const span = document.createElement("span");
        span.className = `err ${e.category || ""}`.trim();
        span.textContent = input.slice(e.start, e.end);

        const tip =
            `原文：${e.original ?? span.textContent}
建議：${e.suggest ?? ""}
原因：${e.reason_zh ?? ""}
規則：${e.rule_zh ?? ""}`.trim();

        span.setAttribute("data-tip", tip);

        container.append(span);
        cursor = e.end;
    }

    if (cursor < input.length) {
        container.append(document.createTextNode(input.slice(cursor)));
    }
}

// ✅ 全域：給 signalr-setup.js / history-render.js 用
window.renderGrammarViz = renderGrammarViz;
window.showBracketMarking = showBracketMarking;

// ✅ 不要再依賴 ui，改成外面傳 DOM 進來
window.showGrammarCheck = function (result, refs) {
    const input = result?.input ?? "";
    const errors = result?.errors || [];

    if (refs?.vizEl) window.renderGrammarViz(input, errors, refs.vizEl);

    window.showBracketMarking(input, errors);

    if (refs?.correctedEl) refs.correctedEl.textContent = result?.corrected ?? "";

    if (refs?.meaningEl) {
        const meaning = result?.meaning_zh ?? result?.meaning_Zh ?? "";
        refs.meaningEl.textContent = meaning || "不適用";
    }
};
