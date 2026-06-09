// document.addEventListener("DOMContentLoaded", function () {

//   const body = document.body;
//   const html = document.documentElement;

//   const themeToggle =
//     document.getElementById("themeToggle");

//   const themeIcon =
//     document.getElementById("themeIcon");

//   // DEFAULT THEME = DARK

//   let savedTheme =
//     localStorage.getItem("theme") || "dark";

//   applyTheme(savedTheme);

//   // REMOVE OLD EVENTS

//   if (themeToggle) {

//     themeToggle.onclick = function () {

//       const isLight =
//         body.classList.contains("light-mode");

//       const newTheme =
//         isLight ? "dark" : "light";

//       applyTheme(newTheme);

//       localStorage.setItem(
//         "theme",
//         newTheme
//       );
//     };
//   }

//   function applyTheme(theme) {

//     if (theme === "light") {

//       body.classList.add("light-mode");

//       html.setAttribute(
//         "data-bs-theme",
//         "light"
//       );

//       if (themeIcon) {

//         themeIcon.className =
//           "bi bi-sun-fill";
//       }
//     }
//     else {

//       body.classList.remove("light-mode");

//       html.setAttribute(
//         "data-bs-theme",
//         "dark"
//       );

//       if (themeIcon) {

//         themeIcon.className =
//           "bi bi-moon-stars-fill";
//       }
//     }
//   }

// });
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

    themeToggle.addEventListener(
      "click",
      function () {

        var isLight =
          body.classList.contains("light-mode");

        var newTheme =
          isLight ? "dark" : "light";

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
