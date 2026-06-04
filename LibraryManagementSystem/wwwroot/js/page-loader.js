// ============================================================
// Global page loader + SweetAlert helper.
// Loader is auto-shown on:
//   - form submissions (any <form method="post">)
//   - browser navigation away (window.beforeunload)
//   - clicks on real <a href> links (not # links, not target=_blank)
// Hidden once the next page's DOMContentLoaded fires.
// Manual control: showPageLoader(msg) / hidePageLoader().
//
// Also wires the swalConfirm() helper so onsubmit="return confirm(...)"
// style guards can be upgraded to themed Swal modals.
// ============================================================

(function () {
    var OVERLAY_ID = "globalPageLoader";

    function ensureOverlay() {
        var el = document.getElementById(OVERLAY_ID);
        if (el) return el;

        el = document.createElement("div");
        el.id = OVERLAY_ID;
        el.className = "page-loader-overlay";
        el.innerHTML =
            '<div class="page-loader-spinner"></div>' +
            '<div class="page-loader-text">Loading <span>BookVerse</span>...</div>';
        document.body.appendChild(el);
        return el;
    }

    window.showPageLoader = function (msg) {
        var el = ensureOverlay();
        if (msg) {
            el.querySelector(".page-loader-text").innerHTML =
                msg + " <span>...</span>";
        }
        el.classList.add("visible");
    };

    window.hidePageLoader = function () {
        var el = document.getElementById(OVERLAY_ID);
        if (el) el.classList.remove("visible");
    };

    // Loader auto-triggers.
    document.addEventListener("DOMContentLoaded", function () {
        ensureOverlay();
        hidePageLoader();

        // Show on any POST form submission.
        document.body.addEventListener("submit", function (evt) {
            var f = evt.target;
            if (f && f.tagName === "FORM") {
                var method = (f.getAttribute("method") || "get").toLowerCase();
                if (method === "post") {
                    showPageLoader("Saving");
                }
            }
        }, true);

        // Show when navigating away via a normal link click.
        document.body.addEventListener("click", function (evt) {
            var a = evt.target.closest && evt.target.closest("a");
            if (!a) return;

            var href = a.getAttribute("href") || "";
            if (!href || href.charAt(0) === "#") return;
            if (a.target === "_blank") return;
            if (a.hasAttribute("data-no-loader")) return;
            if (href.startsWith("mailto:") || href.startsWith("tel:")) return;
            if (href.startsWith("javascript:")) return;

            // Same-page hash navigation — skip.
            try {
                var u = new URL(a.href, window.location.href);
                if (u.origin === window.location.origin &&
                    u.pathname === window.location.pathname &&
                    u.hash) {
                    return;
                }
            } catch (e) { /* relative URL, treat as normal */ }

            showPageLoader("Loading");
        }, true);
    });

    // Show on browser navigation (back / forward / refresh).
    window.addEventListener("beforeunload", function () {
        showPageLoader("Loading");
    });

    // ============================================================
    // SweetAlert confirm helper. Wires up any element with
    //    data-swal-confirm="message"
    // to intercept its native form submission and ask via Swal first.
    // ============================================================

    document.addEventListener("DOMContentLoaded", function () {
        if (typeof Swal === "undefined") return;

        // Intercept submit on forms marked with [data-swal-confirm].
        document.body.addEventListener("submit", function (evt) {
            var f = evt.target;
            if (!f || !f.hasAttribute || !f.hasAttribute("data-swal-confirm")) return;
            if (f.dataset.swalConfirmed === "1") return; // already confirmed

            evt.preventDefault();
            var msg = f.getAttribute("data-swal-confirm") || "Are you sure?";
            var icon = f.getAttribute("data-swal-icon") || "question";
            var confirmBtn = f.getAttribute("data-swal-confirm-text") || "Yes";
            var cancelBtn = f.getAttribute("data-swal-cancel-text") || "Cancel";

            Swal.fire({
                title: msg,
                icon: icon,
                showCancelButton: true,
                confirmButtonText: confirmBtn,
                cancelButtonText: cancelBtn,
                reverseButtons: true
            }).then(function (r) {
                if (r.isConfirmed) {
                    f.dataset.swalConfirmed = "1";
                    showPageLoader("Saving");
                    f.submit();
                }
            });
        }, true);
    });
})();
