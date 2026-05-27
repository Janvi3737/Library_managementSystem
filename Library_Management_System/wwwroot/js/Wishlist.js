document.addEventListener("DOMContentLoaded", function () {

  document.querySelectorAll(".wishlist-btn").forEach(btn => {

    btn.addEventListener("click", async function () {

      const bookId = parseInt(this.dataset.bookid);

      if (!bookId) return;

      const body = new URLSearchParams();
      body.append("bookId", bookId);

      try {

        const response = await fetch('/Member/Wishlist/Toggle', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          },
          body: body
        });

        const result = await response.json();

        if (!result.success) {
          alert(result.message || "Login required");
          return;
        }

        // Update all wishlist buttons everywhere
        document.querySelectorAll(".wishlist-btn").forEach(b => {

          const id = parseInt(b.dataset.bookid);

          const icon = b.querySelector("i");
          const text = b.querySelector(".wishlist-text");

          const isActive =
            result.wishlistIds.includes(id);

          // active class
          b.classList.toggle("active", isActive);

          // icon update
          if (icon) {

            if (isActive) {

              icon.classList.remove("fa-regular");
              icon.classList.add("fa-solid");

            } else {

              icon.classList.remove("fa-solid");
              icon.classList.add("fa-regular");
            }
          }

          // text update (Details page)
          if (text) {

            text.textContent =
              isActive
                ? "Wishlisted"
                : "Add Wishlist";
          }

        });

        // Navbar count update
        const badge =
          document.querySelector(".wishlist-badge");

        if (badge) {

          const count =
            result.wishlistIds.length;

          if (count > 0) {

            badge.innerText = count;
            badge.style.display = "flex";

          } else {

            badge.style.display = "none";
          }
        }

      }
      catch (err) {

        console.log(err);
        alert("Something went wrong");
      }

    });

  });

});
