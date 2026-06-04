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
            'Content-Type': 'application/x-www-form-urlencoded',
            'Accept': 'application/json'
          },
          body: body
        });

        // Anonymous users hit a 302 to /Account/Login which is HTML, not
        // JSON. Detect that up front so .json() doesn't blow up the catch.
        if (!response.ok || (response.redirected && response.url.indexOf('/Login') !== -1)) {
          if (typeof Swal !== 'undefined') {
            Swal.fire({
              icon: 'info',
              title: 'Sign in to use Wishlist',
              text: 'Log in or register to save books to your wishlist.',
              showCancelButton: true,
              confirmButtonText: 'Log in',
              cancelButtonText: 'Not now'
            }).then(r => {
              if (r.isConfirmed) window.location.href = '/Account/Login';
            });
          } else {
            window.location.href = '/Account/Login';
          }
          return;
        }

        const result = await response.json();

        if (!result.success) {
          if (typeof Swal !== 'undefined') {
            Swal.fire({
              icon: 'warning',
              title: 'Could not update wishlist',
              text: result.message || 'Login required',
              confirmButtonColor: '#7c3aed'
            });
          } else {
            alert(result.message || 'Login required');
          }
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
        if (typeof Swal !== 'undefined') {
          Swal.fire({
            icon: 'error',
            title: 'Something went wrong',
            text: 'The wishlist could not be updated. Please try again.',
            confirmButtonColor: '#7c3aed'
          });
        } else {
          alert("Something went wrong");
        }
      }

    });

  });

});
