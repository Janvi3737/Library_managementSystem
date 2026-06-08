document.addEventListener("DOMContentLoaded", function () {

  const body = document.body;
  const html = document.documentElement;

  const themeToggle =
    document.getElementById("themeToggle");

  const themeIcon =
    document.getElementById("themeIcon");

  // DEFAULT THEME = DARK

  let savedTheme =
    localStorage.getItem("theme") || "dark";

  applyTheme(savedTheme);

  // REMOVE OLD EVENTS

  if (themeToggle) {

    themeToggle.onclick = function () {

      const isLight =
        body.classList.contains("light-mode");

      const newTheme =
        isLight ? "dark" : "light";

      applyTheme(newTheme);

      localStorage.setItem(
        "theme",
        newTheme
      );
    };
  }

  function applyTheme(theme) {

    if (theme === "light") {

      body.classList.add("light-mode");

      html.setAttribute(
        "data-bs-theme",
        "light"
      );

      if (themeIcon) {

        themeIcon.className =
          "bi bi-sun-fill";
      }
    }
    else {

      body.classList.remove("light-mode");

      html.setAttribute(
        "data-bs-theme",
        "dark"
      );

      if (themeIcon) {

        themeIcon.className =
          "bi bi-moon-stars-fill";
      }
    }
  }

});
