document.addEventListener("DOMContentLoaded", function () {

  var body =
    document.body;

  var html =
    document.documentElement;

  var themeToggle =
    document.getElementById("themeToggle");

  var themeIcon =
    document.getElementById("themeIcon");

  // DEFAULT DARK

  var savedTheme =
    localStorage.getItem("theme");

  if (!savedTheme) {

    savedTheme = "dark";

    localStorage.setItem(
      "theme",
      "dark"
    );
  }

  applyTheme(savedTheme);

  // TOGGLE

  if (themeToggle) {

    themeToggle.addEventListener("click", function () {

      var isLight =
        body.classList.contains("light-mode");

      var newTheme =
        isLight ? "dark" : "light";

      console.log("Theme button clicked", newTheme);

      applyTheme(newTheme);

      localStorage.setItem(
        "theme",
        newTheme
      );
    });

  }

  // APPLY THEME

  function applyTheme(theme) {

    if (theme === "light") {

      body.classList.add("light-mode");
      body.classList.add("light-theme");

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
      body.classList.remove("light-theme");

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
